# Mediator

The mediator dispatches commands, queries and notifications inside one process. A Roslyn source generator produces the
dispatch code and pre-compiled pipeline delegates at compile time, so there is no reflection, no `MakeGenericType` and
no service provider lookup to resolve a pipeline at runtime.

| Use the mediator when | Use the message bus when |
| --- | --- |
| Dispatch stays in-process | The message crosses a process boundary |
| You want CQRS separation inside a service | You want pub/sub across services |
| You want a middleware pipeline around handlers | You need transport features: retries, outbox, scheduling |

Most services use both: the mediator for the request path inside the service, the bus for events that leave it.

## Rules

1. **Pick the marker interface from the message's role.**

   | Interface | Role | Dispatch | Handlers |
   | --- | --- | --- | --- |
   | `ICommand` | action, no response | `SendAsync` | exactly one |
   | `ICommand<TResponse>` | action with response | `SendAsync` | exactly one |
   | `IQuery<TResponse>` | read, no side effects | `QueryAsync` | exactly one |
   | `INotification` | in-process event | `PublishAsync` | zero or more |

   ```csharp
   public record PlaceOrderCommand(Guid ProductId, int Quantity, string CustomerId)
       : ICommand<PlaceOrderResult>;

   public record GetProductByIdQuery(Guid Id) : IQuery<ProductDto?>;

   public record OrderPlacedNotification(Guid OrderId, decimal Amount) : INotification;
   ```

   Naming follows the same convention as bus messages: commands imperative, queries noun-based, notifications past
   tense ([messages.md](messages.md)).

2. **A query has no side effects.** It is the half of CQRS that can be cached, retried freely and excluded from
   transactions. A "query" that writes is a command, and treating it as a query means the transaction middleware is
   compiled out of its pipeline and the write runs without one.

3. **One handler class per message, one interface per class.** A class implementing several mediator handler interfaces
   is a build error (MO0005), unlike the bus, where the highest-priority interface silently wins. Two handlers for one
   command or query is also an error (MO0002).

   ```csharp
   public sealed class PlaceOrderCommandHandler(AppDbContext db)
       : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
   {
       public async ValueTask<PlaceOrderResult> HandleAsync(
           PlaceOrderCommand command,
           CancellationToken cancellationToken)
       {
           var order = new Order
           {
               Id = Guid.NewGuid(),
               ProductId = command.ProductId,
               Quantity = command.Quantity,
               CustomerId = command.CustomerId
           };

           db.Orders.Add(order);
           await db.SaveChangesAsync(cancellationToken);

           return new PlaceOrderResult(true, order.Id);
       }
   }
   ```

4. **Fix MO0001 and MO0020 rather than suppressing them.** MO0001 says a command or query has no handler; MO0020 says
   something dispatches a message no handler in the assembly can take. Both mean a runtime
   `InvalidOperationException: No pipeline registered for message type`, discovered by a user instead of by the build.
   If the handler lives in another assembly, call that assembly's generated module method and the diagnostic goes away
   by itself.

5. **Inject the narrowest interface.** `ISender` for commands and queries, `IPublisher` for notifications, `IMediator`
   only when one class genuinely needs both. The narrow interface documents what the caller does.

   ```csharp
   app.MapPost("/orders", async (PlaceOrderRequest request, ISender sender) =>
   {
       var result = await sender.SendAsync(
           new PlaceOrderCommand(request.ProductId, request.Quantity, request.CustomerId));

       return result.Success
           ? Results.Created($"/orders/{result.OrderId}", result)
           : Results.BadRequest(result.Error);
   });

   app.MapGet("/products", async (ISender sender) => await sender.QueryAsync(new GetProductsQuery()));
   ```

6. **Avoid the untyped overloads.** `SendAsync(object)` and `PublishAsync(object)` give up compile-time checking and
   throw at runtime when the type implements no marker interface. They exist for dispatch driven by runtime data
   (a deserialized command queue, a plugin). Everything else uses the typed call, which also lets the generator
   validate the call site.

7. **Wrap commands in a transaction with the middleware, not by hand.**

   ```csharp
   builder.Services
       .AddMediator()
       .AddCatalog()
       .AddInstrumentation()
       .UseEntityFrameworkTransactions<CatalogDbContext>();
   ```

   The middleware begins the transaction, calls the handler, commits on success and rolls back on any exception.
   Handlers still call `SaveChangesAsync`; they never call `BeginTransactionAsync`, `CommitAsync` or `RollbackAsync`.
   Queries and notifications are excluded at compile time, so the middleware is not even present in their pipelines.

   Setting `ShouldCreateTransaction` disables that compile-time elimination, and the predicate then runs on every
   dispatch. Use it only when a specific message genuinely needs the opposite of the default.

8. **This is also what makes a mediator command safe with the outbox.** A command handler that publishes to the bus
   inside the mediator transaction gets its envelope written to the outbox in that same transaction, so the in-process
   command and the outgoing event commit together ([reliability.md](reliability.md)).

9. **Keep notifications sequential unless you have measured a reason not to.** `Sequential` is the default: handlers
   run one at a time in registration order, and a throwing handler stops the rest and propagates. `Concurrent` runs
   them with `Task.WhenAll`, collects exceptions into an `AggregateException`, and shares one scoped
   `IServiceProvider` across all of them. A shared `DbContext` used concurrently is a bug waiting for load.

   ```csharp
   builder.Services
       .AddMediator()
       .ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Concurrent)
       .AddCatalog();
   ```

   If handlers must run concurrently and need scoped services, each handler creates its own scope.

10. **Do not use notifications to fake an event bus.** An `INotification` is in-process only, dispatched
    synchronously inside the caller's scope, with no durability, no retry and no delivery guarantee. If the reaction
    must survive a crash or reach another service, publish a bus event instead.

11. **Filter middleware at compile time.** Returning `next` from the factory keeps the middleware out of that pipeline
    entirely: no wrapper, no per-dispatch type check.

    ```csharp
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                if (factoryCtx.IsNotification() || factoryCtx.IsQuery())
                {
                    return next;
                }

                var middleware = new TransactionMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Transaction");
    ```

    `IsCommand()`, `IsCommandWithResponse()`, `IsQuery()`, `IsNotification()`, `IsMessageAssignableTo<T>()` and
    `IsResponseAssignableTo<T>()` are the available predicates. Once the factory has filtered by message type,
    `InvokeAsync` can cast without re-checking. Use runtime checks only for decisions that depend on the instance or on
    configuration.

12. **Resolve scoped services inside the per-message delegate, never in the factory.** The factory runs once at
    startup. A service resolved there is shared by every dispatch, which turns a scoped `DbContext` into a de facto
    singleton. See [middleware.md](middleware.md) for the same trap on the bus side.

13. **Every middleware calls `next`.** A middleware that returns without calling it short-circuits the pipeline and the
    handler never runs, silently. To recover from an exception instead of rethrowing, set `context.Result` to a
    fallback and return normally.

14. **Order middleware deliberately, and name it.** The first registered is the outermost. `Instrumentation` is always
    present and always first, added by `AddMediator()`. Use `before` and `after` with the key when relative position
    matters, and give every middleware a key so others can position against it. An unknown key throws at startup.

    ```csharp
    builder.Services
        .AddMediator()
        .AddCatalog()
        .Use(LoggingMiddleware.Create())
        .Use(ValidationMiddleware.Create())
        .Use(SecurityMiddleware.Create(), before: "Logging");
    ```

15. **Keep handlers `Scoped`.** That is the default, and it matches the lifetime of the `DbContext` and the request
    scope. If a different lifetime is genuinely needed, call `ConfigureOptions` *before* the generated
    `Add{Module}()` method, because that method reads the configured lifetime.

16. **Use a named mediator only for genuinely separate pipelines.** Named mediators use keyed DI, each with its own
    handlers, middleware and runtime. Two bounded contexts in one host with different middleware is a reason; "keeping
    things tidy" is not.

    ```csharp
    builder.Services.AddMediator("billing").AddBilling();
    builder.Services.AddMediator().AddCatalog();
    ```

    ```csharp
    app.MapPost("/payments", async ([FromKeyedServices("billing")] ISender sender, ProcessPaymentRequest request) =>
        Results.Ok(await sender.SendAsync(new ProcessPaymentCommand(request.Amount))));
    ```

17. **Turn on instrumentation.** `AddInstrumentation()` activates the OpenTelemetry listener behind the always-present
    `MediatorDiagnosticMiddleware`, which otherwise uses a no-op listener. Without it, mediator dispatch is invisible in
    traces that otherwise span the whole request ([observability.md](observability.md)).

## Gotchas

- Mediator call sites are excluded from the AOT JSON diagnostics (MO0018), because in-process dispatch does not
  serialize. Bus dispatch of the same type is not.
- Each notification handler gets its own compiled pipeline, so middleware wraps each handler individually rather than
  the publish as a whole.
- Registering a handler manually with `AddHandler<T>()` in addition to the generated module composes configuration
  rather than duplicating the handler, but it is still reflection-based: prefer the generator.
- `AddHandler<T>()` throws `InvalidOperationException` when `T` implements no handler interface.
- A middleware registered after the service provider is built has no effect. The pipeline is compiled once at startup.
