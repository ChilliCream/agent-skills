---
name: mocha-best-practices
description: Mocha messaging best practices for .NET services - message contracts, handlers and consumers, routing and endpoints, outbox/inbox reliability, exception policies, sagas, scheduling, the in-process mediator, and testing. Use when writing or reviewing C# that uses Mocha, including classes implementing IEventHandler, IEventRequestHandler, IBatchEventHandler, IConsumer or Saga<TState>, mediator handlers (ICommandHandler, IQueryHandler, INotificationHandler), and any AddMessageBus / AddMediator builder chain. Triggers on "Mocha", "message bus", "IMessageBus", "PublishAsync", "SendAsync", "RequestAsync", "outbox", "inbox", "saga", "dead letter", "transport", "RabbitMQ", on an MO0001-MO0020 build diagnostic, and on questions about whether something should be an event, a command or a request.
---

# Mocha messaging best practices

You are implementing or reviewing a service that uses [Mocha](https://chillicream.com/docs/mocha), the ChilliCream
messaging framework for .NET. Mocha has two dispatch mechanisms and this skill covers both: the **message bus**
(messages crossing process boundaries over a transport) and the **mediator** (in-process CQRS dispatch). This skill
defines how ChilliCream expects that code to look.

Messaging mistakes are expensive: a wrong pattern choice leaks into every consumer's contract, a missing outbox loses
business events silently, and a handler that is not idempotent corrupts data on the first redelivery. Most of the rules
below exist because the failure they prevent is invisible in development and only shows up under production load.

## Core principles

1. **The pattern follows the question, not convenience.** "Who needs to know?" is an event (`PublishAsync`,
   `IEventHandler<T>`, zero or more handlers). "Who should do this?" is a send (`SendAsync`,
   `IEventRequestHandler<TRequest>`, exactly one handler). "What is the result?" is a request (`RequestAsync`,
   `IEventRequestHandler<TRequest, TResponse>`, exactly one handler, caller waits). A published message that only works
   when exactly one specific handler acts on it is a command in disguise and must be sent, not published.

2. **Contracts are plain records with no framework types.** A message holds business data. Infrastructure metadata
   (message id, correlation, timestamps, custom headers) lives in the envelope and is read through `IConsumer<T>`, never
   copied into the payload. Contracts that cross services live in their own contracts assembly so producer and consumer
   compile against the same type.

3. **Registration is source-generated.** Declare `[assembly: MessagingModule("OrderService")]`, reference
   `Mocha.Analyzers` as an analyzer, and call the generated `AddOrderService()`. Hand-written `AddEventHandler<T>()` and
   friends use runtime reflection and exist for handlers the generator cannot see (plugins, dynamically loaded
   assemblies, tests).

4. **Delivery is at-least-once, so every handler is idempotent or protected by the inbox.** Retry, redelivery, outbox
   re-dispatch and broker redelivery all invoke the same handler again with the same `MessageId`. Either make the
   handler safe to run twice or enable `UsePostgresInbox()` with `UseTransaction()` so Mocha deduplicates per
   `(MessageId, ConsumerType)`.

5. **A handler that writes to the database and publishes a message uses the outbox.** Without it the two writes can
   diverge: committed data with no event, or an event describing state that rolled back. `UsePostgresOutbox()` plus
   `UseTransaction()` persists the envelope in the same transaction as the business data.

6. **Exception policy is explicit per exception type.** Permanent failures (validation, authorization, malformed
   payloads) go straight to `DeadLetter()`. Transient failures get `Retry()` and then `ThenRedeliver()`. A blanket
   retry-everything policy burns the retry budget on failures that can never succeed and delays the ones that can.

7. **Sagas are for processes that span multiple messages over time.** One message in, one action out is a handler.
   A saga must have a path to a final state from every state, a timeout so it cannot wait forever, and a transition for
   every message it can receive, including faults and out-of-order arrivals.

8. **Mediator in-process, bus across boundaries.** Use the mediator for commands, queries and notifications inside one
   service. Use the bus when a message leaves the process. Most services use both.

9. **Observability is opt-in and must be turned on.** `AddInstrumentation()` on the bus (and on the mediator) plus
   `AddSource("Mocha")` and `AddMeter("Mocha")` in the OpenTelemetry configuration. Without both halves there are no
   traces, and "where did my message go?" becomes log archaeology.

10. **Tests drive messages through the in-memory transport and wait on completion signals, never on a sleep.** A
    `Task.Delay` in a messaging test is a race condition with a timer attached.

## Project setup

A service project references the runtime packages plus the source generator, which is analyzer-only:

```xml
<PackageReference Include="Mocha" />
<PackageReference Include="Mocha.Transport.RabbitMQ" />
<PackageReference Include="Mocha.EntityFrameworkCore.Postgres" />
<PackageReference Include="Mocha.Analyzers" />
```

`Mocha.Analyzers` ships its generator under `analyzers/dotnet/cs` and is marked as a development dependency, so a plain
`PackageReference` is enough. Inside this repository, where the generator is consumed as a project reference, it needs
the analyzer plumbing spelled out:

```xml
<ProjectReference Include="..\..\src\Mocha.Analyzers\Mocha.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Name the module explicitly rather than relying on the assembly-name-derived default, in any file in the project:

```csharp
[assembly: MessagingModule("OrderService")]
```

This is the name of the generated registration method (`AddOrderService()`) that registers every handler, consumer and
saga in the assembly. The mediator has its own attribute, `[assembly: MediatorModule("OrderService")]`, and its own
generated method on the mediator builder.

The builder chain reads top to bottom as cross-cutting settings, then handlers, then persistence, then the transport
last:

```csharp
builder.Services
    .AddMessageBus()
    .Host(h => h.ServiceName("order-service"))
    .AddInstrumentation()
    .AddResilience()
    .AddOrderService()
    .AddEntityFramework<OrderDbContext>(p =>
    {
        p.UsePostgresOutbox();   // dispatch side
        p.UseResilience();       // EF execution strategy around the consumer
        p.UseTransaction();      // one transaction per consumed message
        p.UsePostgresInbox();    // deduplication inside that transaction
    })
    .AddRabbitMQ();
```

Set `ServiceName` explicitly. It prefixes every subscribe endpoint, and it is what gives each subscribing service its
own queue for a fan-out event. Falling back to the entry assembly name couples your queue names to a rename.

## Reference file index

Detailed rules with worked wrong-vs-right examples. Load only what the current task touches.

| Reference | When to load |
| --- | --- |
| [references/messages.md](references/messages.md) | Defining or changing a message contract, naming a message, headers, correlation, message type identity (URN), versioning a contract that is already in production |
| [references/patterns.md](references/patterns.md) | Deciding between publish, send and request/reply, request timeouts, acknowledgement, batch processing |
| [references/handlers.md](references/handlers.md) | Any `IEventHandler`, `IEventRequestHandler`, `IBatchEventHandler` or `IConsumer` implementation, DI scoping, idempotency, publishing from inside a handler, handler exception behavior |
| [references/registration.md](references/registration.md) | Builder chains, the source generator, `[assembly: MessagingModule]`, manual registration, an `MO00xx` build diagnostic, AOT and `JsonSerializerContext`, multi-assembly module imports |
| [references/routing.md](references/routing.md) | Endpoint and queue names, service name, `AddMessage<T>()` routes, `Queue(...)`, implicit and explicit binding, error and skipped endpoints, multi-transport routing, temporary endpoints |
| [references/reliability.md](references/reliability.md) | Outbox, inbox, transactions, delivery guarantees, retry, redelivery, dead-letter, circuit breaker, concurrency limits, exception policies |
| [references/scheduling.md](references/scheduling.md) | Delayed or future delivery, `SchedulePublishAsync`/`ScheduleSendAsync`, cancelling a scheduled message, `NotSupportedException` from a scheduled dispatch |
| [references/sagas.md](references/sagas.md) | Any `Saga<TState>`, state machine design, saga state and persistence, timeouts, faults and compensation, parallel coordination, saga correlation |
| [references/mediator.md](references/mediator.md) | `ICommand`, `IQuery`, `INotification` and their handlers, `ISender`/`IPublisher`, mediator middleware, EF Core transactions, notification publish modes |
| [references/middleware.md](references/middleware.md) | Custom dispatch, receive or consumer middleware, pipeline ordering, the middleware factory and DI scoping trap |
| [references/transports.md](references/transports.md) | Choosing a transport, transport-specific endpoint settings (prefetch, batch size), what the InMemory transport does not exercise, health checks, topology provisioning |
| [references/observability.md](references/observability.md) | Tracing and metrics setup, what spans Mocha emits, trace context propagation, custom diagnostic observers, log correlation |
| [references/testing.md](references/testing.md) | Writing or reviewing a test that publishes, sends or consumes a message, testing sagas, testing outbox and inbox flows, deciding between the in-memory transport and a real broker |

## Verifying a change

A green build is not evidence that a handler is wired up. Three generator diagnostics are `Info` severity, which means
the type is excluded from code generation, its queue is never created, nothing consumes the message, and nothing fails:
MO0004 and MO0006 (open generic mediator types) and MO0012 (open generic messaging handler). Raise them in the
project's `.editorconfig` so they cannot pass unnoticed:

```ini
[*.cs]
dotnet_diagnostic.MO0004.severity = warning
dotnet_diagnostic.MO0006.severity = warning
dotnet_diagnostic.MO0012.severity = warning
```

Then confirm what was actually registered, rather than reasoning about the naming conventions:

```csharp
var runtime = host.Services.GetRequiredService<IMessagingRuntime>();

var queues = runtime.Description.Transports
    .SelectMany(t => t.ReceiveEndpoints)
    .Where(e => e.Kind is ReceiveEndpointKind.Default)   // excludes _error, _skipped and reply endpoints
    .Select(e => e.Name)
    .OrderBy(n => n, StringComparer.Ordinal)
    .ToArray();

Assert.Equal(["order-service.order-placed"], queues);
```

`MessageBusDescription` also carries `Consumers`, `MessageTypes`, `Routes` (inbound and outbound) and `Sagas`, where
each saga state lists its transitions and an `IsFinal` flag. That answers "is my handler bound?", "where does this
message go?" and "can this state reach a final state?" without starting a broker.

## Reviewing

When reviewing messaging code, check in this order. The earlier items invalidate the later ones.

1. Is the pattern right ([patterns.md](references/patterns.md))? A pattern mistake is a contract mistake and cannot be
   fixed later without coordinating a deployment.
2. Is the contract right ([messages.md](references/messages.md))? Message shape is published surface.
3. Can the handler run twice safely ([handlers.md](references/handlers.md),
   [reliability.md](references/reliability.md))?
4. Can a message be lost between the database and the transport ([reliability.md](references/reliability.md))?
5. Does every failure have a defined destination, and does every saga have a path out of every state
   ([reliability.md](references/reliability.md), [sagas.md](references/sagas.md))?
6. Would an operator be able to tell where a message went ([observability.md](references/observability.md))?
