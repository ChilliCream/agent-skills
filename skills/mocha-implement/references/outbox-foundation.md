# Outbox and inbox foundation

## Goal

Make a handler that writes to the database and publishes a message atomic, and make redelivery of that message safe.

## Choose what you need

| Situation | Configuration |
| --- | --- |
| Handler only reads, publishes nothing | Nothing. Do not add an outbox you do not need. |
| Handler writes to the database and publishes or sends a message | `UsePostgresOutbox()` plus `UseTransaction()` |
| Every handler is provably idempotent on a business key | Outbox only |
| Handlers are not all idempotent, or duplicates are expensive | Outbox plus `UsePostgresInbox()` |
| A direct publish from an endpoint or background job that also writes | Outbox, plus an explicit transaction at the call site |

| Configuration | Guarantee | What your handlers must do |
| --- | --- | --- |
| Neither | At-most-once | Accept that a crash between receipt and completion loses the message |
| Outbox | At-least-once | Be idempotent |
| Outbox and inbox | Effectively exactly-once | Nothing extra: Mocha deduplicates per `(MessageId, ConsumerType)` |

## Agent enforcement rules

### Do

- Add `UseTransaction()` whenever you add the outbox. Without it a consumed message has no ambient transaction for the
  outbox write to join.
- Register the outbox and inbox tables in the `DbContext` model and create the migration.
- Keep `RequireTransaction` at its default and open a transaction where one is missing.
- Add the inbox unless every handler is provably idempotent, and keep the business write in the same transaction as the
  inbox claim.

### Do not

- Do not publish before the durable write and hope both succeed. That is the dual write the outbox exists to prevent.
- Do not disable `RequireTransaction` to silence `InvalidOperationException`. The exception is the outbox reporting the
  bug.
- Do not assume the inbox makes a handler safe to run twice against a different database. The claim lives next to the
  business data, on the primary.
- Do not add the outbox to a read-only handler. It costs a table write per message for nothing.

## Build checklist

- Add the tables to the model: `modelBuilder.AddPostgresOutbox()` and `modelBuilder.AddPostgresInbox()`.
- Configure the bus: `UsePostgresOutbox()`, `UseResilience()`, `UseTransaction()`, `UsePostgresInbox()`, in that order.
- Create and apply the migration.
- Decide retention for both tables and schedule the cleanup.
- Add a test that redelivers the same `MessageId` twice and asserts one effect.

## Example: configuration

```csharp
builder.Services
    .AddMessageBus()
    .Host(h => h.ServiceName("order-service"))
    .AddResilience()
    .AddOrderService()
    .AddEntityFramework<OrderDbContext>(p =>
    {
        p.UsePostgresOutbox();   // dispatch side: envelopes persisted with the business data
        p.UseResilience();       // EF execution strategy wrapping the consumer
        p.UseTransaction();      // one transaction per consumed message
        p.UsePostgresInbox();    // deduplication inside that transaction
    })
    .AddRabbitMQ();
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddPostgresOutbox();
    modelBuilder.AddPostgresInbox();
}
```

## Example: handler with an atomic write and publish

```csharp
public sealed class OrderPlacedHandler(BillingDbContext db, IMessageBus bus)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        var invoice = new Invoice { OrderId = message.OrderId, Amount = message.TotalAmount };
        db.Invoices.Add(invoice);

        // written to the outbox table inside the ambient transaction
        await bus.PublishAsync(
            new InvoiceCreatedEvent { InvoiceId = invoice.Id, OrderId = message.OrderId },
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }
}
```

The handler needs no transaction code of its own: `UseTransaction()` supplies it. The publish becomes a row, and the
dispatcher sends it after the commit.

## Example: publishing from outside a consumer

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

db.Orders.Add(order);

await bus.PublishAsync(
    new OrderPlacedEvent { OrderId = order.Id, TotalAmount = order.Total },
    cancellationToken);

await db.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

An endpoint or background job has no ambient transaction, so open one. A direct bus publish uses the first `DbContext`
registered with `UsePostgresOutbox()`: a transaction on a different context does not satisfy `RequireTransaction`.

## Guardrails

- The outbox gives at-least-once, never exactly-once on its own. The dispatcher can crash after sending and before
  marking the row sent.
- Ordering is not preserved. Two events published in one transaction may arrive in either order.
- Both tables grow forever without retention. Configure cleanup and monitor the row counts.
- The inbox key is `(MessageId, ConsumerType)`. Two different consumers of the same message both run, which is correct.
- A handler that reads its own writes from a read replica may not see them. The claim and the write commit on the
  primary.

## Default implementation shape

1. Model: outbox and inbox tables registered in `OnModelCreating`, with a migration.
2. Bus: `UsePostgresOutbox()`, `UseResilience()`, `UseTransaction()`, `UsePostgresInbox()`.
3. Handler: business write plus publish, no transaction handling of its own.
4. Operations: retention and cleanup for both tables, plus an alert on the error endpoint.
5. Tests: the write and the publish commit together, a rollback publishes nothing, a duplicate delivery produces one
   effect.

## Verification

```bash
dotnet build
dotnet ef migrations list
dotnet test --filter "FullyQualifiedName~Outbox|FullyQualifiedName~Inbox"
```

Test these against a real PostgreSQL database with the InMemory transport. The behavior under test is transactional,
which a fake store does not reproduce, and the transport is irrelevant to it. An `InvalidOperationException` about a
missing transaction in a test is a finding, not a test setup problem.

## References

- [Reliability](https://chillicream.com/docs/mocha/reliability)
- [Exception policies](https://chillicream.com/docs/mocha/exception-policies)
- [mocha-best-practices: reliability](../../mocha-best-practices/references/reliability.md)
- [mocha-best-practices: handlers](../../mocha-best-practices/references/handlers.md)
