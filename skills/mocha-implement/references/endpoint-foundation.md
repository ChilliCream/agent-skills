# Endpoint foundation

## Goal

Get the queue names and the per-endpoint limits you intend, with the topology declared in configuration rather than at
call sites.

## Agent enforcement rules

### Do

- Set `Host(h => h.ServiceName("..."))` on every bus. It prefixes every subscribe endpoint, which is what gives each
  subscribing service its own queue for a fan-out event.
- Let the conventions name endpoints, and change a name only when you have a reason.
- Put a fixed destination in `AddMessage<T>()`, where it is visible once.
- Use `transport.Handler<T>()` when you only want to tune an endpoint and keep its convention name.
- Check the resulting topology instead of reasoning about the conventions.

### Do not

- Do not pass `SendOptions.Endpoint` for a destination that never changes. A per-call address also bypasses
  provisioning, so the queue is never declared and the broker drops the message silently.
- Do not rename a service, a handler or a contract namespace casually. All three rename broker entities, which needs a
  drain plan.
- Do not raise concurrency without raising prefetch, or the extra slots sit idle.
- Do not scatter endpoint settings across `ConfigureEndpoint` calls and attributes. One place per endpoint.

## Naming conventions

Names derive from the handler and message type names, kebab-cased, with these suffixes stripped: `Handler`, `Consumer`,
`Command`, `Event`, `Message`, `Query`, `Response`.

| Route kind | Name | Example |
| --- | --- | --- |
| Subscribe (events) | `{service-name}.{handler-name}` | `order-service.order-placed` |
| Send and request | message type, no service prefix | `reserve-inventory` |
| Publish (dispatch) | message namespace plus name | `demo.contracts.events.order-placed` |
| Error and skipped | `{endpoint}_error`, `{endpoint}_skipped` | `order-service.order-placed_error` |

A send endpoint is deliberately shared: one `reserve-inventory` queue with one owner. A subscribe endpoint is
deliberately per service, which is what the service-name prefix buys you.

Handler naming is therefore queue naming. `OrderPlacedHandler` and `OrderPlacedEventHandler` both reduce to
`order-placed`, and two contracts whose names differ only by a stripped suffix collide on one endpoint name.

## Build checklist

- Set the service name explicitly.
- Accept the convention name unless an external contract or an existing deployment requires otherwise.
- Size `MaxConcurrency` to what the downstream dependency tolerates, and `MaxPrefetch` to at least that.
- Decide whether the endpoint is per-service (the default for events) or shared.
- Verify the topology after the change.

## Example: tune one endpoint, keep its name

```csharp
builder.Services
    .AddMessageBus()
    .Host(h => h.ServiceName("order-service"))
    .AddOrderService()
    .AddRabbitMQ(transport =>
    {
        transport.Handler<OrderPlacedHandler>()
            .ConfigureEndpoint(e => e.MaxPrefetch(50).MaxConcurrency(10));
    });
```

`Handler<T>()` claims the handler for the transport and hands you the endpoint, without changing its name and without
switching the transport to explicit binding.

## Example: custom queue name

```csharp
builder.Services
    .AddMessageBus()
    .Host(h => h.ServiceName("order-service"))
    .AddOrderService()
    .AddRabbitMQ(transport =>
    {
        transport.BindExplicitly();

        transport.Queue("order-processing")
            .BindImplicitly()
            .Handler<OrderPlacedHandler>();
    });
```

`BindExplicitly()` at transport scope means the queues you declare are the complete receive topology. `BindImplicitly()`
on the queue keeps Mocha generating the source bindings for that queue's handlers, so you change the name without
hand-writing the bindings.

## Example: explicit routes for outgoing messages

```csharp
builder.Services
    .AddMessageBus()
    .AddMessage<OrderPlacedEvent>(m => m.Publish(r => r.ToExchange("custom-orders-exchange")))
    .AddMessage<ProcessPaymentCommand>(m => m.Send(r => r.ToQueue("payment-processing-queue")))
    .AddRabbitMQ();
```

An explicit route wins over the convention. `ToQueue` is point-to-point, `ToExchange` is fan-out, `ToTopic` is
topic-based, and each transport maps those to its native concepts.

## Example: route one handler to a different transport

```csharp
builder.Services
    .AddMessageBus()
    .AddOrderService()
    .AddRabbitMQ(r => r.IsDefaultTransport())
    .AddInMemory(m => m.Handler<ClickStreamHandler>());
```

A claimed handler belongs to the claiming transport regardless of which one is the default. Everything unclaimed goes to
the default.

## Guardrails

- Feature scope is bus, then transport, then endpoint, with the narrowest winning. Set a value once, at the widest scope
  where it is correct.
- Topology is provisioned at startup, so a wrong exchange name or an incompatible binding is a startup failure rather
  than a message that quietly vanishes later.
- The reply endpoint for request/reply is managed by Mocha. Do not declare it. `Temporary()` is for your own queues.
- A handler skipped by a generator diagnostic produces no endpoint at all, so its queue simply does not exist.
- `_error` and `_skipped` endpoints are created for you. Alert on them, because a message arriving there is a message no
  handler will process again.

## Default implementation shape

1. Service name set on the host.
2. Convention-named endpoints, with `Handler<T>().ConfigureEndpoint(...)` for the few that need limits.
3. `Queue(...)` only where a name is externally mandated.
4. `AddMessage<T>()` for any fixed non-conventional destination.
5. Verification against the live topology.

## Verification

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Topology|FullyQualifiedName~Endpoint"
```

Assert on the real topology rather than on the naming rules:

```csharp
var runtime = host.Services.GetRequiredService<IMessagingRuntime>();

var queues = runtime.Description.Transports
    .SelectMany(t => t.ReceiveEndpoints)
    .Where(e => e.Kind is ReceiveEndpointKind.Default)
    .Select(e => e.Name)
    .OrderBy(n => n, StringComparer.Ordinal)
    .ToArray();

Assert.Equal(["order-service.order-placed"], queues);
```

`Description.Routes.Inbound` and `.Outbound` answer "which consumer is bound to this message type" and "where does this
message go". The Nitro topology visualizer shows the same snapshot graphically.

## References

- [Routing and endpoints](https://chillicream.com/docs/mocha/routing-and-endpoints)
- [Transports](https://chillicream.com/docs/mocha/transports)
- [mocha-best-practices: routing](../../mocha-best-practices/references/routing.md)
- [mocha-best-practices: transports](../../mocha-best-practices/references/transports.md)
