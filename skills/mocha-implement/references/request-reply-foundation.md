# Request/reply foundation

## Goal

Add a request that returns a typed response, with the business outcome in the response rather than in an exception.

## Agent enforcement rules

### Do

- Mark the request with `IEventRequest<TResponse>` so the compiler pairs it with its response type.
- Give the response a dedicated type, never a primitive or a shared DTO.
- Model every expected outcome as data on the response: `Success`, `Found`, `FailureReason`.
- Catch `ResponseTimeoutException` at the call site and decide what the caller should do.

### Do not

- Do not use an exception to report an expected business outcome such as "insufficient funds" or "not found".
- Do not return `null` from a request handler. The bus throws `InvalidOperationException`.
- Do not register two handlers for one request type. That is `MO0011`, a build error.
- Do not use request/reply for fire-and-forget work. A caller waiting on a reply it ignores is a coupling you pay for
  on every call.

## Build checklist

- Define the request as `sealed record Xyz : IEventRequest<XyzResponse>` with `required` init-only properties.
- Define the response as its own `sealed record` carrying the outcome, not just the happy-path payload.
- Implement exactly one `IEventRequestHandler<TRequest, TResponse>`.
- Decide the timeout and what the caller does when it expires.
- Remember that a faulted request goes back to the caller and is never redelivered, so the handler gets one attempt per
  call after its retries.

## Example: request, response, handler

```csharp
public sealed record GetProductRequest : IEventRequest<GetProductResponse>
{
    public required Guid ProductId { get; init; }
}

public sealed record GetProductResponse
{
    public required Guid ProductId { get; init; }
    public required bool Found { get; init; }
    public string? Name { get; init; }
    public decimal? Price { get; init; }
}

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

`Found = false` is an answer. Throwing `ProductNotFoundException` instead turns a normal result into a fault, costs the
caller a retry budget, and loses the product id from the response.

## Example: calling it

```csharp
app.MapGet("/products/{id:guid}", async (
    Guid id,
    IMessageBus bus,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await bus.RequestAsync(
            new GetProductRequest { ProductId = id },
            cancellationToken);

        return response.Found
            ? Results.Ok(response)
            : Results.NotFound();
    }
    catch (ResponseTimeoutException)
    {
        logger.LogWarning("Product request timed out for {ProductId}", id);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});
```

A timeout means "no answer arrived in time", not "the work did not happen". The handler may still be running or may
already have committed. Treat the operation as unknown and make it safe to retry.

## Example: one-way send versus request

```csharp
await bus.SendAsync(command, cancellationToken);     // returns once dispatched
await bus.RequestAsync(command, cancellationToken);  // returns once the handler completed
```

`SendAsync` returning is not evidence the handler ran, or that a handler exists. Use `RequestAsync` when the caller
needs acknowledgement that the work completed.

## Guardrails

- Do not reuse one response type across unrelated requests. It accumulates nullable properties until no caller can tell
  which are populated.
- Do not put a long-running process behind a request. Start a saga and reply when it completes
  ([saga-foundation.md](saga-foundation.md)).
- Do not let a request handler publish events without the outbox if it also writes to the database.
- Do not raise the timeout to paper over a slow handler. Find out why it is slow.

## Default implementation shape

1. Contracts: request implementing `IEventRequest<TResponse>`, plus the dedicated response record.
2. Handler: exactly one `IEventRequestHandler<TRequest, TResponse>`, returning a non-null response on every path.
3. Call site: `RequestAsync` with a `try`/`catch (ResponseTimeoutException)` and an explicit caller decision.
4. Tests: successful reply asserted on the response, the "not found" outcome, and the timeout path.

## Verification

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Request"
```

`MO0011` at build time means two handlers claim the same request type. Assert on the returned response in tests, not on
a recorder: the response is the contract, and waiting for it needs no synchronization.

## References

- [Messaging patterns](https://chillicream.com/docs/mocha/messaging-patterns)
- [Handlers and consumers](https://chillicream.com/docs/mocha/handlers-and-consumers)
- [mocha-best-practices: patterns](../../mocha-best-practices/references/patterns.md)
- [mocha-best-practices: messages](../../mocha-best-practices/references/messages.md)
