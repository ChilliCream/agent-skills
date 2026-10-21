# Scheduling foundation

## Goal

Deliver a message at a future time, with a token you can use to cancel it, on a store that actually exists.

## Agent enforcement rules

### Do

- Schedule a message instead of holding a handler open or running a polling loop.
- Use `SchedulePublishAsync` and `ScheduleSendAsync` when the message may need cancelling, and persist the returned
  token next to the business entity.
- Confirm the transport supports scheduling, and configure `UsePostgresScheduling()` when it does not.
- Re-check the current state when the scheduled message arrives. The world moved on while it waited.

### Do not

- Do not assume a scheduled message is still wanted when it fires. Validate, then act.
- Do not treat a `false` return from `CancelScheduledMessageAsync` as an error. It means the message already fired or
  was already cancelled.
- Do not schedule months ahead against a contract you intend to change. The payload has to deserialize into whatever
  the code looks like then.
- Do not use the `ScheduledTime` option when you need to cancel later. That form returns no token.

## Transport support

| Transport | Native scheduling | Cancellable |
| --- | --- | --- |
| InMemory | yes, not durable | yes |
| PostgreSQL | yes, durable | yes |
| Azure Service Bus | yes, durable | yes |
| RabbitMQ | **none** | only with `UsePostgresScheduling()` |

On RabbitMQ a scheduled dispatch without a configured store throws `NotSupportedException`. The same store backs saga
`Timeout()` and `ScheduleTimeout()`, and backs delayed redelivery, so a missing store disables more than it looks.

## Build checklist

- Decide whether the message must be cancellable.
- Configure the store if the transport needs one, register the table in the `DbContext` model, and create the migration.
- Persist the token transactionally with the entity it belongs to, when the message is cancellable.
- Make the scheduled handler re-validate before acting, and idempotent.
- Test with `FakeTimeProvider` rather than by waiting.

## Example: schedule, persist the token, cancel

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

A token you did not store is a message you cannot cancel. Store it in the same transaction as the entity, or a crash
between the two leaves a reminder nobody can stop.

## Example: fire and forget

```csharp
await bus.PublishAsync(
    message,
    new PublishOptions { ScheduledTime = when },
    cancellationToken);
```

Use this only when cancellation will never be needed.

## Example: the store

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddPostgresScheduledMessages();
}

builder.Services
    .AddMessageBus()
    .AddOrderService()
    .AddEntityFramework<AppDbContext>(p => p.UsePostgresScheduling())
    .AddRabbitMQ();
```

## Example: scheduling from a saga

```csharp
descriptor
    .Initially()
    .OnEvent<OrderPlacedEvent>()
    .StateFactory(e => new OrderState { OrderId = e.OrderId })
    .ScheduledPublish(
        TimeSpan.FromMinutes(30),
        state => new OrderReminderEvent { OrderId = state.OrderId })
    .TransitionTo(AwaitingPayment);
```

Declared in the state machine, the runtime owns the token, the correlation and the cancellation when the saga leaves the
state.

## Guardrails

- A time in the past dispatches immediately rather than failing.
- The scheduled handler is an ordinary handler: at-least-once, so idempotent.
- The Postgres store retries a failed dispatch up to 10 times and records the reason in `last_error`. Query that column
  when a scheduled message never arrives.
- A saga `Timeout()` configured with no store is skipped silently, which means the saga runs with no deadline at all.
- Scheduling is not a work queue. A thousand scheduled messages firing in the same second is a thundering herd, so
  spread them.

## Default implementation shape

1. Store: configured for the transport, in the model, with a migration.
2. Call site: `SchedulePublishAsync`/`ScheduleSendAsync`, token persisted with the entity when cancellable.
3. Cancellation path: wherever the business reason for the message disappears.
4. Handler: re-validates current state, idempotent.
5. Tests: fires at the deadline using `FakeTimeProvider`, cancellation prevents delivery, a cancel after firing returns
   `false` without throwing.

## Verification

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Schedul"
```

A `NotSupportedException` from a scheduled dispatch means the transport has no native scheduling and no store is
configured. In tests, advance a `FakeTimeProvider` instead of waiting for wall-clock time, and assert on the recorder
signal.

## References

- [Scheduling](https://chillicream.com/docs/mocha/scheduling)
- [Transports](https://chillicream.com/docs/mocha/transports)
- [mocha-best-practices: scheduling](../../mocha-best-practices/references/scheduling.md)
- [mocha-best-practices: sagas](../../mocha-best-practices/references/sagas.md)
