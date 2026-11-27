# Handlers and consumers

A handler is business logic with a message-shaped signature. Everything else (retries, transactions, deduplication,
tracing, concurrency, fault routing) is pipeline work and must not appear in handler code.

| Interface | Use when |
| --- | --- |
| `IEventHandler<T>` | Reacting to a published event. |
| `IEventRequestHandler<TRequest>` | Handling a one-way command. |
| `IEventRequestHandler<TRequest, TResponse>` | Handling a request and returning a typed response. |
| `IBatchEventHandler<T>` | Processing a group of events for throughput. |
| `IConsumer<T>` | The handler needs envelope metadata, headers or features. |

## Rules

1. **One handler class, one interface, one message type.** Dependencies come in through the primary constructor, the
   method is the whole handler:

   ```csharp
   public sealed class OrderPlacedHandler(
       BillingDbContext db,
       ILogger<OrderPlacedHandler> logger)
       : IEventHandler<OrderPlacedEvent>
   {
       public async ValueTask HandleAsync(
           OrderPlacedEvent message,
           CancellationToken cancellationToken)
       {
           db.Invoices.Add(new Invoice { OrderId = message.OrderId, Amount = message.TotalAmount });
           await db.SaveChangesAsync(cancellationToken);

           logger.LogInformation("Invoice created for order {OrderId}", message.OrderId);
       }
   }
   ```

   A class implementing several messaging handler interfaces compiles and produces no diagnostic, but only the
   highest-priority interface is registered (`IBatchEventHandler` > `IConsumer` >
   `IEventRequestHandler<T, TResponse>` > `IEventRequestHandler<T>` > `IEventHandler`). The rest are silently dead
   code. Split them.

2. **Handlers are concrete and non-generic or they are not registered at all.** An abstract class carrying the handler
   interface reports MO0013 (warning) and an open generic handler reports MO0012 (info), and in both cases the
   generator *skips* the type. The build still succeeds and the message is never consumed. Put the interface on the
   concrete leaf class and keep shared logic in an interface-free base.

3. **Assume the handler runs more than once for the same message.** At-least-once delivery, immediate retry,
   redelivery, outbox re-dispatch and broker redelivery all re-invoke it. Either make the work idempotent, or enable
   the inbox ([reliability.md](reliability.md)).

   ```csharp
   // wrong: a second delivery charges twice
   var payment = new Payment { OrderId = message.OrderId, Amount = message.TotalAmount };
   db.Payments.Add(payment);
   await db.SaveChangesAsync(cancellationToken);
   ```

   ```csharp
   // right: keyed on the business identity, second delivery is a no-op
   var exists = await db.Payments
       .AnyAsync(p => p.OrderId == message.OrderId, cancellationToken);

   if (exists)
   {
       return;
   }

   db.Payments.Add(new Payment { OrderId = message.OrderId, Amount = message.TotalAmount });
   await db.SaveChangesAsync(cancellationToken);
   ```

   The database must enforce it too: a unique index on the business key turns a concurrent double delivery into a
   constraint violation instead of two rows. Idempotency built only from a read-then-write check in application code is
   a race.

4. **Let exceptions propagate.** The pipeline decides what a failure means: retry, redeliver, dead-letter, or fault the
   caller. A handler that swallows an exception reports success and the message is gone.

   ```csharp
   // wrong
   try { await ChargeAsync(message, ct); }
   catch (Exception ex) { logger.LogError(ex, "charge failed"); }
   ```

   Throw a specific exception type instead, and map it to a policy in `AddResilience` so the behavior is configured in
   one place rather than hidden in a catch block. Catch locally only to add context and rethrow, or to convert an
   expected outcome into part of a response.

   What each handler kind does with an exception:

   - Event and send handlers: the pipeline retries per policy, then redelivers, then routes to the error endpoint.
   - Request handlers: the failure goes back to the caller, which throws. No redelivery.
   - Batch handlers: the whole batch faults as one unit.

5. **Honor the `CancellationToken`.** Pass it to every async call. On shutdown or a timeout the token is cancelled, and
   a handler that ignores it keeps a slot occupied and the host waiting.

6. **A request handler never returns `null`.** The bus throws `InvalidOperationException` on a null response. Model
   absence in the response type (`Found`, `Success`, a nullable payload property), because "not found" is an answer the
   caller needs to receive.

   ```csharp
   public sealed class GetProductRequestHandler(CatalogDbContext db)
       : IEventRequestHandler<GetProductRequest, GetProductResponse>
   {
       public async ValueTask<GetProductResponse> HandleAsync(
           GetProductRequest request,
           CancellationToken cancellationToken)
       {
           var product = await db.Products
               .AsNoTracking()
               .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

           return new GetProductResponse
           {
               ProductId = request.ProductId,
               Found = product is not null,
               Name = product?.Name,
               Price = product?.Price
           };
       }
   }
   ```

7. **Reach for `IConsumer<T>` only when the envelope is needed.** Headers, `MessageId`, correlation identifiers,
   `DeliveryCount`, `SourceAddress` and context features are on `IConsumeContext<T>`. Business logic that needs only
   the payload uses a handler interface, which keeps the signature honest about its inputs.

8. **Scoped services are safe to inject; each message gets its own scope.** Mocha creates a DI scope per message,
   resolves the handler from it, and disposes it when the handler completes. Two messages in flight do not share a
   `DbContext`.

   What is not safe: a singleton holding per-message state, a static mutable field, and a captured `DbContext` inside
   middleware ([middleware.md](middleware.md)).

9. **Publishing from a handler goes through the injected `IMessageBus` and, when the handler also writes, through the
   outbox.** Correlation and causation are inherited automatically, so no identifiers need threading by hand. With
   `UsePostgresOutbox()` and `UseTransaction()` configured, the publish inside the handler is persisted in the same
   transaction as the business data, and dispatched after commit.

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

   Without a transaction the outbox throws by default (`RequireTransaction`), which is the correct failure: it means
   the dual-write it exists to prevent would have happened.

10. **A handler does one thing.** Writing to the database and calling an HTTP API and publishing three events in one
    handler means a partial failure leaves an unknowable state, and a retry repeats the parts that already succeeded.
    Split the work so each handler owns one effect, chained by events, and make each step idempotent.

11. **Do not call `Task.Delay` or `Thread.Sleep` to wait for something.** Scheduling a message is the supported way to
    do work later ([scheduling.md](scheduling.md)); redelivery is the supported way to wait for a dependency to
    recover. A sleeping handler holds a concurrency slot and a transaction.

12. **Keep handler-side observability structured.** Log with message templates and include the business key
    (`{OrderId}`), not an interpolated string. The trace context is already attached by the pipeline, so log lines
    inside a handler correlate to the consume span automatically ([observability.md](observability.md)).

## Retry-aware handlers

`RetryFeature` on the context exposes `ImmediateRetryCount` and `DelayedRetryCount`, which allows graceful degradation
on later attempts. It is `null` when `AddResilience()` is not configured, so always null-check:

```csharp
var state = context.Features.Get<RetryFeature>();

if (state is { ImmediateRetryCount: > 2 })
{
    await ProcessViaFallbackGatewayAsync(context.Message, cancellationToken);
    return;
}
```

Use this for a real fallback path, not to hand-roll a retry loop.

## Batch handler configuration

The source generator registers a batch handler, and batch options are tuned on top of it:

```csharp
builder.Services
    .AddMessageBus()
    .AddOrderService()
    .AddBatchHandler<OrderPlacedBatchHandler>(opts =>
    {
        opts.MaxBatchSize = 50;
        opts.BatchTimeout = TimeSpan.FromSeconds(10);
    })
    .AddRabbitMQ();
```

Defaults are 100 messages and a 1-second timeout. Size the batch to what one database round trip can absorb, and keep
the timeout short enough that a quiet period does not stall processing.

## Gotchas

- `SendAsync` returning does not mean the handler ran. See [patterns.md](patterns.md).
- A handler registered for an interface or base type receives every registered message that implements it. That is the
  polymorphic dispatch path, and it surprises people who added a base type for convenience.
- `DeliveryCount` on the context reflects transport-level delivery attempts and is not the same as
  `RetryFeature.ImmediateRetryCount`. Do not use either as a business counter; both reset in ways you do not control.
- A handler that reads its own writes from a replica may not see them. The inbox claim and the business write are in one
  transaction on the primary; a read-only replica is not part of that guarantee.
