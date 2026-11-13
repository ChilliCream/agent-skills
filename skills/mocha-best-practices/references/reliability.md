# Reliability

Handlers throw, brokers restart, databases fail over, and messages arrive twice. Mocha handles all of that in the
pipeline, which means reliability is a configuration decision made once per service, not a pattern repeated in every
handler.

```csharp
builder.Services
    .AddMessageBus()
    .AddResilience(policy =>
    {
        policy.On<ValidationException>().DeadLetter();
        policy.On<DbException>(ex => !ex.IsTransient).DeadLetter();
        policy.On<DbException>(ex => ex.IsTransient).Retry(5, TimeSpan.FromMilliseconds(200)).ThenRedeliver();
        policy.Default().Retry().ThenRedeliver();
    })
    .AddCircuitBreaker(opts => { opts.FailureRatio = 0.5; opts.BreakDuration = TimeSpan.FromSeconds(30); })
    .AddConcurrencyLimiter(opts => opts.MaxConcurrency = 10)
    .AddOrderService()
    .AddEntityFramework<OrderDbContext>(p =>
    {
        p.UsePostgresOutbox();
        p.UseResilience();
        p.UseTransaction();
        p.UsePostgresInbox();
    })
    .AddRabbitMQ();
```

## What to turn on

| Situation | Configuration |
| --- | --- |
| The handler only reads and dispatches nothing | Nothing. Do not pay for an outbox you do not need. |
| The handler writes to the database and publishes or sends | `UsePostgresOutbox()` plus `UseTransaction()` |
| Every handler is provably idempotent on a business key | Outbox only |
| Handlers are not all idempotent, or a duplicate is expensive | Outbox plus `UsePostgresInbox()` |
| A publish from an endpoint or background job that also writes | Outbox, plus an explicit transaction at the call site |

## Delivery guarantees

| Configuration | Guarantee | Consequence for your handlers |
| --- | --- | --- |
| Neither | At-most-once | A crash between receipt and completion loses the message |
| Outbox | At-least-once | Handlers may run more than once; they must be idempotent |
| Outbox and inbox | Effectively exactly-once | Mocha deduplicates per `(MessageId, ConsumerType)` |

At-least-once is the right default. Decide explicitly which of the two ways to survive a duplicate you are using, and
write it down in the service's configuration rather than assuming.

## Rules

1. **A handler that writes to the database and dispatches a message uses the outbox.** This is the dual-write problem:
   commit the data and fail the publish and the event is lost; publish and roll back and the event describes state that
   never existed. The outbox writes the envelope in the same transaction as the business data, and a background
   processor dispatches it after commit.

   ```csharp
   public class AppDbContext : DbContext
   {
       public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
       public DbSet<Order> Orders => Set<Order>();
   }
   ```

   ```csharp
   .AddEntityFramework<AppDbContext>(p =>
   {
       p.UsePostgresOutbox();
       p.UseTransaction();
   })
   ```

   `UseTransaction()` is what gives a consumed message an ambient transaction; without it a handler has no transaction
   for the outbox write to join.

2. **Do not weaken `RequireTransaction` to make an error go away.** By default a `PublishAsync` or `SendAsync` with the
   outbox enabled and no open transaction throws `InvalidOperationException` and writes nothing. That exception is the
   outbox telling you the dual-write you were about to perform is unsafe. Open a transaction instead:

   ```csharp
   await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

   db.Orders.Add(order);
   await bus.PublishAsync(new OrderPlacedEvent { OrderId = order.Id, /* ... */ }, cancellationToken);
   await db.SaveChangesAsync(cancellationToken);

   await transaction.CommitAsync(cancellationToken);
   ```

   `RequireTransaction = false` is defensible for a message with no accompanying write (a heartbeat, a pure
   notification from a background job). Set it per options name, not globally by reflex:

   ```csharp
   builder.Services.Configure<PostgresOutboxOptions>(
       typeof(AppDbContext).FullName!,
       options => options.RequireTransaction = false);
   ```

   Consumers get their transaction from `UseTransaction()` and mediator handlers from
   `UseEntityFrameworkTransactions<TContext>()`. A direct bus publish uses the first DbContext registered with
   `UsePostgresOutbox()`; a transaction on a different DbContext does not satisfy the requirement.

3. **Add the inbox unless every handler is provably idempotent.** The inbox claims `(MessageId, ConsumerType)` inside
   the handler's transaction, so the claim and the business write commit or roll back together. A redelivery finds the
   claim and skips the handler.

   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.AddPostgresInbox();
   }
   ```

   ```csharp
   .AddEntityFramework<AppDbContext>(p =>
   {
       p.UseTransaction();
       p.UsePostgresInbox();
   })
   ```

   Deduplication is per consumer type, so two handlers for the same event each process it exactly once. A message with
   no `MessageId` passes through without deduplication.

4. **The inbox requires `UseTransaction()` to mean anything.** Without a transaction the claim can commit while the
   business write fails, and the message is then permanently marked processed and permanently not done. Register them
   together, in this order: `UseTransaction()` then `UsePostgresInbox()`.

5. **Size inbox retention to the longest redelivery window of the transport.** Default retention is 7 days with hourly
   cleanup. If a transport or a redelivery schedule can bring a message back after retention has expired, the claim is
   gone and the handler runs again.

   ```csharp
   p.UsePostgresInbox(opts =>
   {
       opts.RetentionPeriod = TimeSpan.FromDays(14);
       opts.CleanupInterval = TimeSpan.FromMinutes(30);
   });
   ```

6. **Classify exceptions. Never retry what cannot succeed.**

   ```csharp
   .AddResilience(policy =>
   {
       policy.On<ValidationException>().DeadLetter();          // permanent, keep for inspection
       policy.On<UnauthorizedAccessException>().DeadLetter();
       policy.On<JsonException>().DeadLetter();
       policy.On<DuplicateMessageException>().Discard();       // safe to lose
       policy.On<HttpRequestException>(ex => ex.StatusCode == HttpStatusCode.TooManyRequests)
           .Redeliver([TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)]);
       policy.Default().Retry().ThenRedeliver();
   })
   ```

   Matching respects inheritance and the most specific type wins, like `catch` blocks. Predicate overloads let a type
   split by condition (transient or not, status code, inner exception). With several predicate rules on one type, the
   first match wins, so order them specific to general.

   `DeadLetter()` preserves the message on the error endpoint for inspection. `Discard()` drops it with no audit trail,
   so reserve it for messages that are genuinely safe to lose.

7. **Retry is for seconds, redelivery is for minutes.** Retry re-runs the handler in-process while holding the
   concurrency slot, so it suits a deadlock or a brief timeout. Redelivery returns the message to the transport,
   releases the slot, and brings the message back later, which suits a dependency that is deploying or rate-limiting.

   ```csharp
   policy.Default()
       .Retry(5, TimeSpan.FromSeconds(1), RetryBackoffType.Exponential)
       .ThenRedeliver([TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30)]);
   ```

   Retry defaults: 3 attempts, 200 ms base delay, exponential, jitter on, 30 s cap. Redelivery defaults: 5, 15 and 30
   minutes, jitter on, 1 hour cap.

8. **Count the total attempts before you tune the numbers.** Retry and redelivery multiply:

   ```text
   total handler invocations = (retry attempts + 1) x (redelivery attempts + 1)
   ```

   The defaults give 16 invocations, not 6. Raising both "to be safe" produces a handler that runs dozens of times over
   hours, against a side effect that may not be idempotent. "My handler runs more times than expected" is almost always
   this.

9. **Know where each tier lives, because it changes the semantics.** Retry is in the consumer pipeline: only handler
   failures are retried (never deserialization or routing failures, which are permanent), and with several consumers on
   one endpoint only the failing consumer retries. Redelivery is in the receive pipeline: one decision per message
   regardless of consumer count, and the message re-enters the full pipeline on its next delivery.

10. **`AddResilience()` must be called for any policy to apply.** A consumer-level policy with no bus-level or
    consumer-level `AddResilience()` registers nothing, because the middleware is not in the pipeline. Policies replace
    rather than merge: a consumer-level policy discards the bus-level rules for that consumer, so repeat the rules you
    still want.

11. **Set the concurrency limit to what the slowest shared resource can take.** The default is
    `Environment.ProcessorCount * 2`, which is usually wrong in both directions: too high for a handler holding a
    database connection from a small pool or calling a rate-limited API, too low for pure I/O waiting.

    ```csharp
    .AddConcurrencyLimiter(opts => opts.MaxConcurrency = 10)
    ```

    Scope precedence is endpoint over transport over bus. Disable it with `opts.Enabled = false` only when something
    else bounds the work.

12. **Use the circuit breaker to stop hammering a broken dependency.**

    ```csharp
    .AddCircuitBreaker(opts =>
    {
        opts.FailureRatio = 0.5;
        opts.MinimumThroughput = 10;
        opts.SamplingDuration = TimeSpan.FromSeconds(10);
        opts.BreakDuration = TimeSpan.FromSeconds(30);
    })
    ```

    There are two breakers and they protect different things: the receive-level `CircuitBreaker` watches handler
    failures, and the transport-level `TransportCircuitBreaker` runs first and watches broker connectivity with a lower
    default ratio. Leave the transport breaker alone unless you are diagnosing broker behavior.

13. **Expire messages that go stale rather than processing them late.** `ExpirationTime` on `PublishOptions` or
    `SendOptions` maps to the envelope's `DeliverBy`, and the expiry middleware drops the message on arrival before any
    deserialization or handler work, marking it consumed. No exception, no dead-letter.

    ```csharp
    await bus.SendAsync(
        new ProcessPaymentCommand { OrderId = orderId, Amount = 99.99m },
        new SendOptions { ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(30) },
        cancellationToken);
    ```

    Use it for time-sensitive work (a price quote, a one-time code). Do not use it as a crude retry limit: an expired
    message disappears without a trace in the error queue.

14. **Alert on the error and skipped queues.** `_error` means a handler threw and the message survived for inspection;
    `_skipped` means nothing consumed the message, which usually means a missing registration or a contract mismatch.
    A messaging system with no monitoring on these is a system with no failure detection. The dead-letter middleware
    logs at `Critical` when it catches an exception, so that log level is worth alerting on too.

15. **Add `UseResilience()` when the handler touches EF Core.** It wraps the consumer pipeline in the DbContext's
    execution strategy, so transient connection drops and deadlocks are retried at the database level, where the
    retry belongs.

## Sagas and retry

Sagas are consumers, so retries and redeliveries apply with no extra configuration. Each saga handler invocation runs
inside a saga transaction, so a failed transition commits nothing and the next attempt loads fresh state. The Postgres
saga store uses optimistic concurrency, and a concurrent write retries automatically after reloading. See
[sagas.md](sagas.md).

## Skipping outbox or inbox for one message

Both middlewares check a context feature, set from a middleware placed before them:

```csharp
builder.Services
    .AddMessageBus(bus =>
    {
        bus.UseConsume(
            new ConsumerMiddlewareConfiguration(
                static (_, next) => ctx =>
                {
                    ctx.Features.GetOrSet<InboxMiddlewareFeature>().SkipInbox = true;
                    return next(ctx);
                },
                "SkipInboxCheck"),
            before: "Inbox");
    })
```

This is a sharp tool. Legitimate uses are a transport with native exactly-once delivery and high-volume messages where
duplicate processing is harmless. Reaching for it to make a test pass means the test found a real design problem.

## Gotchas

- The outbox dispatches only `Publish`, `Send`, `Reply` and `Fault` message kinds; other kinds pass straight through.
- Replies and faults are written without requiring a transaction.
- Redelivery needs scheduling infrastructure. On RabbitMQ that means `UsePostgresScheduling()`, or redelivery cannot
  schedule anything; see [scheduling.md](scheduling.md).
- Request/reply messages are never redelivered, by design. Immediate retry still applies.
- The fault middleware marks a message consumed after handling it, which is what stops the dead-letter middleware from
  forwarding it a second time. A custom middleware that swallows exceptions breaks this chain.
- `RetryFeature` is `null` when `AddResilience()` is not configured, so handler code that reads it must null-check.
