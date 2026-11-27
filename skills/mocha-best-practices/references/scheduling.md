# Scheduling

Scheduling hands a message to the bus with a future delivery time. It is the supported way to do something later, and
it replaces timers, `Task.Delay`, polling loops and "check for due work every minute" background jobs.

## Rules

1. **Schedule a message instead of waiting in process.** A `Task.Delay` in a handler holds a concurrency slot and a
   transaction, and it is lost on restart. A scheduled message is persisted by the store and survives the process.

   ```csharp
   var result = await bus.SchedulePublishAsync(
       new PaymentReminderEvent { OrderId = order.Id },
       DateTimeOffset.UtcNow.AddHours(24),
       cancellationToken);
   ```

2. **Use the `Schedule*Async` methods when the message may need cancelling, and the options form when it never will.**
   `SchedulePublishAsync` and `ScheduleSendAsync` return a `SchedulingResult` with a `Token` and `IsCancellable`.
   Setting `ScheduledTime` on `PublishOptions` or `SendOptions` schedules the message but returns no result, so it
   cannot be cancelled.

   ```csharp
   // cancellable
   var result = await bus.ScheduleSendAsync(command, when, cancellationToken);

   // fire and forget, not cancellable
   await bus.PublishAsync(
       message,
       new PublishOptions { ScheduledTime = when },
       cancellationToken);
   ```

3. **Persist the token with the business entity, not in memory.** A token in a field or a cache is gone after a
   restart, and so is the ability to cancel.

   ```csharp
   var result = await bus.SchedulePublishAsync(
       new PaymentReminderEvent { OrderId = order.Id },
       DateTimeOffset.UtcNow.AddHours(24),
       cancellationToken);

   if (result.IsCancellable)
   {
       order.ReminderToken = result.Token;
       await orders.SaveAsync(order, cancellationToken);
   }
   ```

   ```csharp
   // the customer paid: revoke the reminder
   if (order.ReminderToken is not null)
   {
       await bus.CancelScheduledMessageAsync(order.ReminderToken, cancellationToken);
       order.ReminderToken = null;
       await orders.SaveAsync(order, cancellationToken);
   }
   ```

4. **Treat a `false` from `CancelScheduledMessageAsync` as information, not an error.** It means the message is no
   longer in the store: already dispatched, already cancelled, or an unknown token. Cancelling twice is safe. Clear
   your stored token either way, and make the receiving handler tolerate a reminder that arrives for an order that has
   already been paid, because the cancellation window can always close first.

5. **A scheduled handler must still be idempotent and must re-check its precondition.** The decision to schedule was
   made in the past; the world has moved on. A reminder handler checks whether the payment arrived, a cleanup handler
   checks whether the session is still expired.

6. **Know whether your transport can schedule at all.** PostgreSQL, InMemory and Azure Service Bus each register their
   own store automatically. RabbitMQ has none, and a scheduled dispatch on it throws `NotSupportedException` unless the
   Postgres fallback is registered.

   | Transport | Scheduling | Durability | Cancellation | Setup |
   | --- | --- | --- | --- | --- |
   | InMemory | Native | Lost on restart | Yes | none |
   | PostgreSQL | Native | Durable | Yes | none |
   | RabbitMQ | None | Durable via fallback | Yes, via fallback | `UsePostgresScheduling()` plus the EF model |
   | Azure Service Bus | Native | Durable | Yes | none |

   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.AddPostgresScheduledMessages();
   }
   ```

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()
       .AddEntityFramework<AppDbContext>(p => p.UsePostgresScheduling())
       .AddRabbitMQ();
   ```

   Then add the EF Core migration. Scheduling does not silently fall back to immediate delivery: with no store it
   throws, which is the correct failure mode for a feature you believed you had.

7. **Redelivery and saga timeouts depend on the same infrastructure.** Redelivery schedules the next delivery
   ([reliability.md](reliability.md)), and a saga `Timeout()` schedules a `SagaTimedOutEvent`. A saga timeout with no
   store available is skipped silently: the saga proceeds with no deadline and simply never times out
   ([sagas.md](sagas.md)). On RabbitMQ, `UsePostgresScheduling()` is therefore not optional once you use either
   feature.

8. **Do not schedule far-future work that a contract change will outlive.** A message scheduled a year out will be
   deserialized by a future deployment. If the type was renamed, moved or removed, the dispatcher cannot resolve it and
   the message retries until it is exhausted. For long horizons, store the intent in your own table and schedule short
   hops, or keep the contract frozen and documented as such.

9. **Schedule side effects from a saga with the saga's own methods.** `ScheduledPublish` and `ScheduledSend` are
   available on transition and lifecycle descriptors, which keeps the scheduling inside the state machine where its
   cancellation on completion is handled for you:

   ```csharp
   descriptor.Initially()
       .OnEvent<OrderPlacedEvent>()
       .StateFactory(e => new OrderState { OrderId = e.OrderId })
       .ScheduledPublish(
           TimeSpan.FromMinutes(30),
           state => new OrderReminderEvent { OrderId = state.OrderId })
       .TransitionTo(AwaitingPayment);
   ```

10. **A scheduled time in the past dispatches immediately.** Compute the time from `TimeProvider` (or
    `DateTimeOffset.UtcNow`) at the moment of scheduling and never from a stored value that may already have passed,
    unless immediate delivery is what you want.

## Operating the Postgres store

The dispatcher retries a failed dispatch with exponential backoff up to 10 attempts, records the reason in
`last_error`, and logs at `Critical` once attempts are exhausted. The warnings name the cause: the body could not be
deserialized, the message type is not registered in the dispatching instance (a rename, or a rolling deployment that
has not reached every instance), or no transport can reach the stored destination.

```sql
SELECT id, scheduled_time, times_sent, last_error
FROM scheduled_messages
WHERE times_sent >= max_attempts;
```

Row-level locking means exactly one instance dispatches each message, so multiple instances are safe. The InMemory
store neither retries nor records errors: a failed dispatch is dropped and logged.

## Gotchas

- Scheduled messages never reach the outbox: the scheduler intercepts them, and the background worker dispatches them
  directly to the transport. Do not expect an outbox row for a scheduled publish.
- The InMemory store lives in process memory, so anything pending is lost on shutdown. That is fine for tests and wrong
  for anything else.
- Cancellation tokens are opaque and prefixed by their source (`in-memory-transport:`, `postgres-transport:`,
  `postgres-scheduler:`). Do not parse them; store them as returned.
- `IsCancellable` is true whenever a scheduling call succeeds, so a successful call always gives you a usable token.
- Azure Service Bus cancels natively through the broker, and returns `false` once the message has been enqueued for
  delivery.
