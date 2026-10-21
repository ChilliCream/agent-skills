# Mediator foundation

## Goal

Add an in-process command, query or notification with exactly one handler per message and no accidental coupling to the
bus.

## Agent enforcement rules

### Do

- Choose the marker by intent: `ICommand` changes state, `ICommand<T>` changes state and returns a result, `IQuery<T>`
  reads, `INotification` tells other parts of the same process.
- Keep exactly one handler per command and query. Notifications take many.
- Inject the narrowest interface the call site needs: `ISender` to send, `IPublisher` to publish, `IMediator` only when
  both are used.
- Use `UseEntityFrameworkTransactions<TContext>()` when handlers write to the database.

### Do not

- Do not give a query a side effect. A query that writes makes caching, retries and read replicas unsafe.
- Do not use the mediator to reach another process. That is the bus.
- Do not use notifications as an event bus between features. Two features coupled through an in-process notification are
  still coupled, with the dependency now invisible.
- Do not use the untyped `SendAsync(object)` overload when the type is known at compile time. It defers to runtime what
  the compiler could check.

## Choose the marker

| Marker | Handler | Count | Returns |
| --- | --- | --- | --- |
| `ICommand` | `ICommandHandler<TCommand>` | exactly one | nothing |
| `ICommand<TResponse>` | `ICommandHandler<TCommand, TResponse>` | exactly one | `TResponse` |
| `IQuery<TResponse>` | `IQueryHandler<TQuery, TResponse>` | exactly one | `TResponse` |
| `INotification` | `INotificationHandler<TNotification>` | zero or more | nothing |

## Build checklist

- Define the message as a `record` carrying its marker interface.
- Write one handler, resolved from the per-request DI scope.
- Register with `[assembly: MediatorModule("ServiceName")]` and the generated `Add*()` on the mediator builder.
- Add `UseEntityFrameworkTransactions<TContext>()` if a handler writes.
- Decide the notification publish mode deliberately if you add notifications.

## Example: command, query, notification

```csharp
public record PlaceOrderCommand(Guid ProductId, int Quantity, string CustomerId)
    : ICommand<PlaceOrderResult>;

public record GetProductByIdQuery(Guid Id) : IQuery<ProductDto?>;

public record OrderPlacedNotification(Guid OrderId, decimal Amount) : INotification;
```

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

## Example: registration and call sites

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()                                      // generated from [assembly: MediatorModule("Catalog")]
    .AddInstrumentation()
    .UseEntityFrameworkTransactions<CatalogDbContext>();
```

```csharp
app.MapPost("/orders", async (PlaceOrderRequest request, ISender sender) =>
{
    var result = await sender.SendAsync(
        new PlaceOrderCommand(request.ProductId, request.Quantity, request.CustomerId));

    return result.Success
        ? Results.Created($"/orders/{result.OrderId}", result)
        : Results.BadRequest(result.Error);
});

app.MapGet("/products/{id:guid}", async (Guid id, ISender sender) =>
    await sender.QueryAsync(new GetProductByIdQuery(id)) is { } product
        ? Results.Ok(product)
        : Results.NotFound());
```

`SendAsync` is for commands, `QueryAsync` for queries. The split is not cosmetic: it is what lets middleware treat reads
and writes differently.

## Example: notification publish mode

```csharp
builder.Services
    .AddMediator()
    .ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Concurrent)
    .AddCatalog();
```

`Sequential` is the default and runs handlers in order, so one failure stops the rest. `Concurrent` runs them together
and is only safe when the handlers share no state and none of them depends on another having run.

## Guardrails

- `MO0002` and `MO0005` are errors: two handlers for one message, or one class implementing several mediator handler
  interfaces.
- `MO0001` and `MO0020` are warnings meaning a message has no handler. Dispatching it will fail at runtime, so treat
  them as errors in review.
- A mediator handler that writes to the database and must also publish a bus message needs the outbox
  ([outbox-foundation.md](outbox-foundation.md)). Mediator transactions and the bus outbox both hinge on the same
  `DbContext`.
- Middleware must call `next` unless it is deliberately short-circuiting, and must rethrow rather than swallow.
- The mediator is registered `Scoped`. Resolve it per request, never from the root provider.

## Default implementation shape

1. Message: `record` with the marker interface that matches its intent.
2. Handler: one per command or query, constructor-injected dependencies, resolved per request.
3. Registration: `[assembly: MediatorModule]`, generated `Add*()`, transactions middleware when writing.
4. Call site: `ISender` or `IPublisher`, typed overloads only.
5. Tests: handler called with the expected message, result asserted, failure path covered.

## Verification

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Mediator|FullyQualifiedName~Handler"
```

Read the build output for `MO0001`, `MO0002`, `MO0005` and `MO0020`. Mediator tests need no host and no transport:
build a service provider, resolve `ISender` from a scope, and assert on the returned value.

## References

- [Mediator](https://chillicream.com/docs/mocha/mediator)
- [Mediator pipeline and middleware](https://chillicream.com/docs/mocha/mediator/pipeline-and-middleware)
- [mocha-best-practices: mediator](../../mocha-best-practices/references/mediator.md)
- [mocha-best-practices: middleware](../../mocha-best-practices/references/middleware.md)
