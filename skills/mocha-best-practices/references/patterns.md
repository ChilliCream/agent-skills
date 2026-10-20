# Messaging patterns

Three patterns, three questions. Choosing the wrong one is the most expensive mistake in a messaging system, because the
choice is encoded in the contract, in the topology, and in every consumer that already subscribed.

| Question | Pattern | Bus method | Handler | Handlers | Caller waits |
| --- | --- | --- | --- | --- | --- |
| Who needs to know? | Event (pub/sub) | `PublishAsync` | `IEventHandler<T>` | zero or more | no |
| Who should do this? | Send (fire and forget) | `SendAsync` | `IEventRequestHandler<TRequest>` | exactly one | no |
| What is the result? | Request/reply | `RequestAsync` | `IEventRequestHandler<TRequest, TResponse>` | exactly one | yes |

## Rules

1. **Publish a fact, send an instruction.** An event states that something happened and the publisher does not care who
   reacts, or whether anyone does. A send tells one specific capability to do one specific thing.

   ```csharp
   // right: a fact, anyone may react
   await bus.PublishAsync(
       new OrderPlacedEvent { OrderId = order.Id, /* ... */ },
       cancellationToken);

   // right: an instruction for exactly one capability
   await bus.SendAsync(
       new ReserveInventoryCommand { OrderId = order.Id, ProductId = productId, Quantity = 3 },
       cancellationToken);
   ```

2. **A message that only works when one specific handler acts on it is a send in disguise. Never publish it.**

   ```csharp
   // wrong: "event" that the system depends on exactly one service consuming
   await bus.PublishAsync(new ChargeCustomerEvent { OrderId = id, Amount = total }, ct);
   ```

   ```csharp
   // right
   await bus.SendAsync(new ChargeCustomerCommand { OrderId = id, Amount = total }, ct);
   ```

   Why it matters beyond semantics: a published event fans out, so adding a second subscriber silently double-charges,
   and a published event with no subscriber is discarded without error, so a missing deployment fails silently. A send
   goes to one queue with one owner.

3. **Zero subscribers for a published event is legal and silent.** Nothing fails, nothing is dead-lettered on the
   publishing side. If "nobody handled it" must be an error, the message is not an event: send it, or request it.

4. **`SendAsync` returns after dispatch, not after processing.** It completes when the transport has accepted the
   message. If the caller needs to know the handler finished, use `RequestAsync`, which waits for the handler's
   acknowledgement, and throws on the caller side if the handler fails.

   ```csharp
   await bus.SendAsync(command, ct);     // dispatched
   await bus.RequestAsync(command, ct);  // handler completed (automatic acknowledgement)
   ```

   Do not approximate "wait for processing" with a delay, a poll of a database flag, or a published
   "command accepted" event.

5. **Request/reply is a synchronous dependency with a queue in the middle. Use it sparingly.** Every `RequestAsync`
   makes the caller's latency and availability depend on the responder. In a request path that fans out to several
   services, prefer publishing an event and letting the work happen asynchronously, or read from a local projection.

   Legitimate uses: a lookup the caller cannot proceed without, a validation that must happen before a response is
   returned to a user, a saga asking a service to do work and report back.

6. **Always handle `ResponseTimeoutException` at a request call site.**

   ```csharp
   try
   {
       var response = await bus.RequestAsync(
           new ProcessRefundCommand { OrderId = orderId, Amount = 49.99m, /* ... */ },
           cancellationToken);

       return Results.Ok(response);
   }
   catch (ResponseTimeoutException)
   {
       logger.LogWarning("Refund request timed out for order {OrderId}", orderId);
       return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
   }
   ```

   A timeout means the responder is not running, has no registered handler, or is slower than the window. All three
   happen in production. An unhandled timeout turns into a 500 and an alert with no information.

7. **Request/reply is not redelivered.** Immediate retry still applies, but a failed request does not get scheduled for
   a later attempt, because the caller is waiting. The caller receives a `NotAcknowledgedEvent`-driven failure rather
   than a timeout. Design the caller to handle failure now, not to rely on the message coming back later. See
   [reliability.md](reliability.md).

8. **One handler per request type.** Two `IEventRequestHandler` implementations for the same request is a build error
   (MO0011), because the reply would be non-deterministic. Fan-out is what events are for.

9. **Chain events, do not orchestrate with them.** A handler that completes its work and publishes the next fact is a
   healthy chain: `OrderPlaced` leads to `PaymentCompleted` leads to `ShipmentShipped`, each service reacting to what it
   cares about.

   ```csharp
   public sealed class OrderPlacedEventHandler(BillingDbContext db, IMessageBus bus)
       : IEventHandler<OrderPlacedEvent>
   {
       public async ValueTask HandleAsync(
           OrderPlacedEvent message,
           CancellationToken cancellationToken)
       {
           var payment = await ChargeAsync(message, cancellationToken);

           db.Payments.Add(payment);

           await bus.PublishAsync(
               new PaymentCompletedEvent
               {
                   PaymentId = payment.Id,
                   OrderId = message.OrderId,
                   Amount = message.TotalAmount,
                   ProcessedAt = DateTimeOffset.UtcNow
               },
               cancellationToken);

           await db.SaveChangesAsync(cancellationToken);
       }
   }
   ```

   When the flow needs a decision that depends on several replies, or has to wait and compensate, that is not an event
   chain any more. It is a process, and it belongs in a saga ([sagas.md](sagas.md)).

10. **Publishing order is not delivery order.** Two events published in sequence may be consumed in either order, by
    different services, at different times. Any logic that depends on ordering must either be driven by one message
    carrying everything it needs, or tolerate out-of-order arrival explicitly (idempotent upserts keyed by a version,
    or a saga with transitions for both arrival orders).

11. **Multiple handlers for the same event are independent.** They run in unspecified order, each in its own consumer
    pipeline with its own retries, and one failing does not prevent the others. Do not rely on handler A having run
    before handler B. If B needs A's result, B reacts to the event A publishes.

12. **Reach for a batch handler only when batching is the point.** `IBatchEventHandler<T>` trades latency and
    failure granularity for throughput: a whole batch faults together.

    ```csharp
    public sealed class OrderPlacedBatchHandler(AnalyticsDbContext db)
        : IBatchEventHandler<OrderPlacedEvent>
    {
        public async ValueTask HandleAsync(
            IMessageBatch<OrderPlacedEvent> batch,
            CancellationToken cancellationToken)
        {
            db.RevenueSummaries.Add(new RevenueSummary
            {
                OrderCount = batch.Count,
                TotalRevenue = batch.Sum(order => order.TotalAmount),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }
    ```

    Good fits: bulk inserts, aggregation, analytics ingestion. Bad fits: anything where one poisoned message must not
    take 99 healthy ones to the error queue with it, and anything that must be processed as soon as it arrives.
    `IMessageBatch<T>` is an `IReadOnlyList<T>`; `CompletionMode` says whether the batch closed on `Size`, `Time` or
    `Forced` (endpoint shutting down), and `batch.GetContext(index)` gives the envelope for one element.

## Deciding between a message and a method call

Not everything needs a message. In-process work with no durability requirement is a method call or a mediator dispatch
([mediator.md](mediator.md)). Use the bus when the message leaves the process, must survive a restart, or must fan out
to subscribers the caller does not know about.

## Gotchas

- `RequestAsync` has an overload that takes a message with no `IEventRequest<T>`, which waits for an acknowledgement
  rather than a typed response. That is the "send and wait" tool, not a way to get data back.
- A send route is shared: every service sending `ReserveInventoryCommand` dispatches to the same `reserve-inventory`
  queue, which is the point-to-point guarantee. A subscribe route is per service, because each subscriber needs its own
  queue. See [routing.md](routing.md).
- Both an `IEventHandler<T>` and an `IConsumer<T>` can be registered for the same message type and both will run.
  That is intentional, and it also means an audit consumer added "just to log" doubles the consumer count on that
  endpoint.
- A class that implements several messaging handler interfaces is registered under exactly one of them, by priority:
  `IBatchEventHandler` wins over `IConsumer`, which wins over `IEventRequestHandler<T, TResponse>`, then
  `IEventRequestHandler<T>`, then `IEventHandler`. No diagnostic is reported, so the other interfaces are silently
  dead. One handler class, one interface.
