---
name: mocha-implement
description: Task recipes for building Mocha messaging features in a .NET service - add an event handler or consumer, add a request/reply flow, add a saga, turn on the outbox and inbox, name and tune an endpoint, schedule a message, add a mediator command or query, and write the test that proves it works. Use when the user wants something built or wired up rather than reviewed; for reviewing existing Mocha code use mocha-best-practices. Each recipe gives a goal, enforcement rules, a build checklist, a worked example, guardrails, the default implementation shape, and verification commands. Triggers on "add a handler", "add a consumer", "create a saga", "wire up the outbox", "schedule a message", "add a command", "add a query", "publish an event", and on a new file implementing IEventHandler, IEventRequestHandler, IBatchEventHandler, IConsumer, Saga<TState>, ICommandHandler, IQueryHandler or INotificationHandler.
---

# Implement Mocha messaging

This skill is a runbook. Pick the one recipe that matches the change, apply it, then run its verification commands.

The recipes describe the shape Mocha expects. They do not describe the application you are editing: the target
project's existing conventions, its referenced `Mocha` package version, and the Mocha source or documentation all take
precedence where they conflict with a recipe. Read the project before writing into it.

Use [mocha-best-practices](../mocha-best-practices/SKILL.md) for the reasoning behind these rules, for reviewing code
that already exists, and for the topics no recipe covers (message contract design, routing conventions, middleware,
transports, observability).

## Task to recipe map

| The request | Recipe |
| --- | --- |
| Add or change an event handler, a one-way command handler, a batch handler, a raw consumer | [references/handler-foundation.md](references/handler-foundation.md) |
| Add a request that returns a typed response, call one, handle its timeout | [references/request-reply-foundation.md](references/request-reply-foundation.md) |
| Coordinate several messages over time, add or change a state machine, compensate a failed step | [references/saga-foundation.md](references/saga-foundation.md) |
| Stop losing events on a crash, deduplicate redeliveries, add transactions | [references/outbox-foundation.md](references/outbox-foundation.md) |
| Name a queue, change concurrency or prefetch, claim a handler for a transport | [references/endpoint-foundation.md](references/endpoint-foundation.md) |
| Deliver a message later, cancel a pending one, add a saga deadline | [references/scheduling-foundation.md](references/scheduling-foundation.md) |
| Add an in-process command, query or notification | [references/mediator-foundation.md](references/mediator-foundation.md) |
| Write or fix a test for any of the above | [references/testing-foundation.md](references/testing-foundation.md) |

A request that spans two recipes has one primary: the artifact being created. Adding a saga that needs a deadline is
the saga recipe, with the scheduling recipe consulted for the store it requires. Borrow from a secondary recipe only
where the two agree.

## Workflow

1. Read the project first: the `Mocha*` package versions, the existing builder chain, one neighbouring handler. Match
   what is there.
2. Select one primary recipe from the map.
3. Decide the messaging pattern before writing any type. "Who needs to know?" is an event, "who should do this?" is a
   send, "what is the result?" is a request. The choice is published surface and cannot be changed later without a
   coordinated deployment.
4. Apply the recipe. Keep the diff minimal and leave intentional existing patterns alone.
5. Add tests for the success path and for the failure path the recipe names (timeout, fault, duplicate delivery).
6. Run the verification commands below plus the ones in the recipe, and read the build output for `MO` diagnostics.

## Global rules

- Message contracts are `sealed record` types with `required` init-only properties, no framework types, in a contracts
  assembly when they cross a service boundary.
- Registration is source-generated. Declare `[assembly: MessagingModule("ServiceName")]`, reference `Mocha.Analyzers`,
  and call the generated `AddServiceName()`. Hand-written `AddEventHandler<T>()` is for handlers the generator cannot
  see.
- Delivery is at-least-once. Every handler is either idempotent or protected by the inbox.
- Configuration belongs in the builder chain, not at the call site. A fixed destination goes in `AddMessage<T>()`, not
  in `SendOptions.Endpoint`.
- Exceptions propagate out of handlers. The pipeline decides whether a failure retries, redelivers or dead-letters, and
  that decision is declared once in `AddResilience()`.
- Transport assumptions stay explicit. Name the transport the code targets, because scheduling, ordering and
  provisioning differ between them.

## Do

- Set `Host(h => h.ServiceName("..."))` explicitly on any new bus. It prefixes every subscribe endpoint and decides
  whether two services share a queue or each get their own.
- Keep one handler to one business responsibility and one effect.
- Put the handler interface on a concrete, non-generic, non-abstract class.
- Give every saga a timeout and a path from every state to a final state.
- Add the inbox alongside the outbox unless every handler is provably idempotent.

## Do not

- Do not publish an event that the system depends on exactly one service consuming. That is a command, and it must be
  sent.
- Do not write to the database and publish a message in the same handler without the outbox.
- Do not use exceptions for expected business outcomes in a request/reply flow. Model the outcome in the response type.
- Do not synchronize a test with `Task.Delay`. Wait on a signal.
- Do not implement several messaging handler interfaces on one class. It compiles, produces no diagnostic, and silently
  registers only the highest-priority one.
- Do not invent a new abstraction when a recipe covers the case.

## Verification

Run these from the solution or project directory after any change:

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Messaging"
```

Then read the build output for `MO` diagnostics. They are the fastest signal that a handler did not register:

| Diagnostic | Severity | Meaning |
| --- | --- | --- |
| MO0002, MO0005, MO0011, MO0014, MO0015, MO0016 | Error | Build fails, fix as reported |
| MO0001, MO0003, MO0013, MO0018, MO0020 | Warning | Usually a handler or message that will never be dispatched |
| MO0004, MO0006, MO0012 | **Info** | A handler or message type was skipped by the generator and the build is still green |

The three Info rules are the dangerous ones: the type is excluded from code generation, the queue is never created, and
nothing fails. Raise them in the project's `.editorconfig` so they cannot pass unnoticed:

```ini
[*.cs]
dotnet_diagnostic.MO0004.severity = warning
dotnet_diagnostic.MO0006.severity = warning
dotnet_diagnostic.MO0012.severity = warning
```

To confirm what actually got registered, assert on the runtime topology rather than reasoning about the conventions:

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

`MessageBusDescription` also exposes `Consumers`, `MessageTypes`, `Routes` and `Sagas`, which answers "is my handler
bound?" and "where does this message go?" without starting a broker.

## References

- [Mocha documentation](https://chillicream.com/docs/mocha)
- [Handlers and consumers](https://chillicream.com/docs/mocha/handlers-and-consumers)
- [Messaging patterns](https://chillicream.com/docs/mocha/messaging-patterns)
- [Reliability](https://chillicream.com/docs/mocha/reliability)
- [Sagas](https://chillicream.com/docs/mocha/sagas)
