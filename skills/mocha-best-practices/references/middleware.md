# Middleware and pipelines

Everything in Mocha is a pipeline. Three of them, each with its own context and its own job:

```text
PublishAsync / SendAsync / RequestAsync
  Dispatch pipeline      IDispatchContext   unserialized message, headers, destination
        |  transport
  Receive pipeline       IReceiveContext    raw envelope, transport metadata, routing
        |
  Consumer pipeline      IConsumeContext    deserialized message, per-message DI scope
        |
  Your handler
```

The default middleware covers retries, redelivery, faults, dead-lettering, expiry, circuit breaking, concurrency
limiting, transactions, the inbox and instrumentation. Write your own only for something genuinely cross-cutting that
none of those covers.

## Rules

1. **Pick the pipeline from what the middleware needs to see.**

   | Need | Pipeline |
   | --- | --- |
   | Stamp a header on every outgoing message | Dispatch |
   | Validate or enrich before serialization | Dispatch |
   | Rate-limit or log every inbound envelope | Receive |
   | Transaction or unit of work per handler | Consumer |
   | Time or audit one handler invocation | Consumer |
   | Tenant context from headers | Receive (before `Routing`) or Consumer |

   A concern placed in the wrong pipeline either runs too early to see the typed message or too late to affect routing.

2. **Resolve services inside the per-message delegate, never in the factory.** The factory lambda runs once at startup
   to build the delegate chain. Anything resolved there is shared by every message.

   ```csharp
   // wrong: db behaves as a singleton for the lifetime of the process
   var db = serviceProvider.GetRequiredService<AppDbContext>();
   return new ConsumerMiddlewareConfiguration(
       (context, next) => ctx => middleware.InvokeAsync(ctx, next, db),
       "UnitOfWork");
   ```

   ```csharp
   // right: resolved from the per-message scope
   return new ConsumerMiddlewareConfiguration(
       static (context, next) =>
       {
           var middleware = new UnitOfWorkConsumerMiddleware();
           return ctx => middleware.InvokeAsync(ctx, next);
       },
       "UnitOfWork");
   ```

   ```csharp
   internal sealed class UnitOfWorkConsumerMiddleware
   {
       public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
       {
           var db = context.Services.GetRequiredService<AppDbContext>();

           await using var tx = await db.Database.BeginTransactionAsync();
           try
           {
               await next(context);
               await tx.CommitAsync();
           }
           catch
           {
               await tx.RollbackAsync();
               throw;
           }
       }
   }
   ```

   Prefer `context.Services` inside `InvokeAsync` over constructing per-message middleware instances. A middleware
   object allocated per message is per-message garbage on the hot path.

3. **Always rethrow.** Swallowing an exception in middleware tells the pipeline the message succeeded, so the fault
   middleware never routes it to the error endpoint and the message is gone. The rollback-then-rethrow in the example
   above is the pattern: clean up, then let it propagate.

4. **Always call `next`, unless short-circuiting is the whole point.** A middleware that forgets `next` silently skips
   everything downstream including the handler. Deliberate short-circuits exist (the expiry middleware drops stale
   messages that way), and they should be obvious from the middleware's name and its log output.

5. **Give every middleware a key and position it explicitly.** The first registered is outermost: it runs first on the
   way in and last on the way out. `UseReceive`, `UseDispatch` and `UseConsume` take `before` or `after` (not both) to
   place a middleware relative to a named one.

   ```csharp
   builder.Services
       .AddMessageBus(bus =>
       {
           bus.UseDispatch(TenantDispatchMiddleware.Create("acme"), before: "Instrumentation");
           bus.UseReceive(LoggingReceiveMiddleware.Create(), after: "ReceiveInstrumentation");
           bus.UseConsume(UnitOfWorkConsumerMiddleware.Create());
       })
       .AddOrderService()
       .AddRabbitMQ();
   ```

   Useful anchors: `Instrumentation`, `ReceiveInstrumentation`, `ConcurrencyLimiter`, `DeadLetter`, `Fault`,
   `CircuitBreaker`, `Expiry`, `MessageTypeSelection`, `Routing`, `Serialization`, `Inbox`. Appending with no anchor
   puts your middleware after the built-in defaults, which is rarely what a cross-cutting concern wants.

6. **Register middleware before the service provider is built.** The pipeline is compiled once at startup into a single
   delegate chain. Middleware added later has no effect, with no error.

7. **Scope middleware as narrowly as it applies.** Bus-level middleware runs on every endpoint of every transport,
   transport-level on one transport, endpoint-level on one endpoint, and the most specific scope wins. A middleware that
   only matters for one endpoint does not belong at the bus level.

8. **Do not reimplement built-in middleware.** A hand-written retry loop, a custom dedupe table, a "log and continue"
   error handler or a manual transaction wrapper all conflict with the pipeline that already does that job, and the two
   interact in ways nobody will reason about during an incident. Configure the built-in feature instead
   ([reliability.md](reliability.md)).

9. **Use features for cross-middleware state, not for smuggling data into handlers.** `context.Features` is how
   middleware communicates with middleware (`InboxMiddlewareFeature`, `OutboxMiddlewareFeature`, `RetryFeature`), and
   pooled features reset between messages. Data a handler needs belongs in the message or, for infrastructure values,
   in a header the handler reads from `IConsumeContext`.

10. **Keep middleware allocation-free on the hot path.** Use `static` lambdas for the factory, avoid closures over
    per-message state, and do not build strings for logs that will be filtered out. Every message pays for every
    middleware in its pipeline.

## The receive pipeline order

Worth knowing, because it explains which failures are retried and which are not:

```text
TransportCircuitBreaker
  ConcurrencyLimiter
    ReceiveInstrumentation
      DeadLetter
        Fault
          Redelivery
            CircuitBreaker
              Expiry
                MessageTypeSelection
                  Routing
                    Consumer pipeline
                      ConsumerInstrumentation
                        Retry
                          Transaction (optional)
                            Inbox (optional)
                              Your handler
```

Retry sits inside the consumer pipeline, so only handler failures are retried and only the failing consumer retries.
Redelivery sits in the receive pipeline, so there is one decision per message and the concurrency slot is released
while waiting. The inbox sits after the transaction, so the claim commits with the handler's business data.

## Gotchas

- Dispatch and receive run in different processes for every transport except InMemory. A dispatch middleware cannot see
  what the consumer did.
- Receive middleware runs before message type resolution, so the typed message is not available there. If you need the
  payload, you want consumer middleware.
- A middleware registered at the bus level also wraps Mocha's own internal messages (replies, faults, health checks,
  scheduled dispatches). Guard on message kind or endpoint rather than assuming every message is yours.
- `before:` and `after:` with an unknown key throws at startup. That is better than silently appending, so do not
  work around it by appending instead.
