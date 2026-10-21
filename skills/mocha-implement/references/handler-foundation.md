# Handler foundation

## Goal

Add a handler that consumes one message type, is safe to run twice, and carries no infrastructure code.

## Agent enforcement rules

### Do

- Pick the narrowest interface that fits: `IEventHandler<T>` for an event, `IEventRequestHandler<TRequest>` for a
  one-way command, `IBatchEventHandler<T>` for throughput, `IConsumer<T>` only when the envelope is needed.
- Take dependencies through the primary constructor and keep the handler method as the whole handler.
- Make the work idempotent, keyed on a business identity enforced by a unique index.
- Let the source generator register the handler, and verify it did.

### Do not

- Do not implement two messaging handler interfaces on one class. Only the highest-priority one is registered and the
  rest are silently dead code.
- Do not catch and log an exception the pipeline should see.
- Do not put the handler interface on an abstract or open generic class.
- Do not combine unrelated effects (database write, HTTP call, three publishes) in one handler.

## Build checklist

- Define the contract as a `sealed record` with `required` init-only properties.
- Create one concrete, non-generic handler class per message type.
- Name it for the message plus the role: `OrderPlacedHandler`, `ReserveInventoryHandler`. The name becomes the queue
  name, so it is published surface.
- Guard the effect against a second delivery, or enable the inbox
  ([outbox-foundation.md](outbox-foundation.md)).
- Pass the `CancellationToken` to every async call.
- Confirm the handler appears in the generated registration (no `MO0003`, `MO0012`, `MO0013`).

## Example: event handler

```csharp
public sealed record OrderPlacedEvent
{
    public required Guid OrderId { get; init; }
    public required decimal TotalAmount { get; init; }
}

public sealed class OrderPlacedHandler(
    BillingDbContext db,
    ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        var exists = await db.Invoices
            .AnyAsync(i => i.OrderId == message.OrderId, cancellationToken);

        if (exists)
        {
            return;
        }

        db.Invoices.Add(new Invoice { OrderId = message.OrderId, Amount = message.TotalAmount });
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Invoice created for order {OrderId}", message.OrderId);
    }
}
```

## Example: registration

```csharp
// anywhere in the project
[assembly: MessagingModule("BillingService")]

builder.Services
    .AddMessageBus()
    .Host(h => h.ServiceName("billing-service"))
    .AddInstrumentation()
    .AddResilience()
    .AddBillingService()        // generated: registers every handler, consumer and saga
    .AddRabbitMQ();
```

`AddBillingService()` is generated from the module attribute. Use `AddEventHandler<T>()` only for a handler the
generator cannot see, such as one in a dynamically loaded assembly or defined in a test.

## Example: consumer when the envelope is needed

```csharp
public sealed class OrderAuditConsumer(AuditDbContext db) : IConsumer<OrderPlacedEvent>
{
    public async ValueTask ConsumeAsync(IConsumeContext<OrderPlacedEvent> context)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            MessageId = context.MessageId,
            CorrelationId = context.CorrelationId,
            DeliveryCount = context.DeliveryCount,
            OrderId = context.Message.OrderId
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
```

`ConsumeAsync` takes only the context: the cancellation token is `context.CancellationToken`. Reach for `IConsumer<T>`
only when the envelope is needed. Business logic that needs only the payload takes a handler interface, which keeps the
signature honest about its inputs.

## Guardrails

- Do not return early on an exception you swallowed. Throw a specific type and map it in `AddResilience()`.
- Do not use `DeliveryCount` or `RetryFeature` counts as a business counter. Both reset in ways you do not control.
- Do not block with `Thread.Sleep` or `Task.Delay`. Schedule a message or let redelivery wait for you.
- Do not register a handler for a base type or interface unless polymorphic dispatch is what you want: it will receive
  every registered message that implements it.
- Do not inject a singleton that holds per-message state. Mocha gives every message its own DI scope, so scoped
  services are the correct choice.

## Default implementation shape

1. Contract: `sealed record` with `required` members, in the contracts assembly if it crosses a boundary.
2. Handler: one concrete class, one interface, one message type, dependencies via primary constructor.
3. Registration: `[assembly: MessagingModule]` plus the generated `Add*()` call in the builder chain.
4. Tests: delivery succeeds, duplicate delivery is a no-op, failure path reaches the configured destination.

## Verification

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Handler"
```

Check the build output for `MO0003`, `MO0012` and `MO0013`. Each means the handler was skipped by the generator, so no
queue exists and the message is never consumed, while the build stays green.

## References

- [Handlers and consumers](https://chillicream.com/docs/mocha/handlers-and-consumers)
- [Handler registration](https://chillicream.com/docs/mocha/handler-registration)
- [mocha-best-practices: handlers](../../mocha-best-practices/references/handlers.md)
- [mocha-best-practices: messages](../../mocha-best-practices/references/messages.md)
