# QUERIES — `[QueryType]`

A query class is the thinnest possible adapter: route the GraphQL arguments to a Mocha `IQuery<T>` via `ISender.QueryAsync` and return what it returns.

## Anatomy

```csharp
using Mocha.Mediator;

[QueryType]
public class BookQueries
{
    public async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid id,
        CancellationToken ct)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
}
```

Rules:

- `[QueryType]` on the class. HotChocolate composes all `[QueryType]` classes across the assembly into a single root `Query` type.
- One `<Entity>Queries.cs` per entity. Put it in `<Entity>/Operations/`.
- The method body is one line — `sender.QueryAsync(new <X>Query(...), ct)`. No business logic.
- Inject `ISender` and dispatch a Mocha `IQuery<T>`. Never a `DbContext`, never a DataLoader directly. The query handler encapsulates authorization, batching, and projection. See [`query`](../../query/SKILL.md).
- Return `ValueTask<T>` — that matches Mocha's surface and avoids unnecessary `.AsTask()` adapters.

## Parameters

| Parameter | Purpose |
|---|---|
| `ClaimsPrincipal user` | The authenticated user/principal. Required for any data access. Pass it into the query record so the handler can authorize against it. |
| `[ID<Entity>] Guid id` | A Relay global ID. HotChocolate decodes the global ID into a domain `Guid`. The generic form ties the ID to the type. |
| `ISender sender` | The Mocha dispatcher. Use `QueryAsync` for reads. |
| `CancellationToken ct` | Required, no default. |
| `[Service] IFoo foo` | Explicit service injection. Use only when the type might otherwise be ambiguous (rare). |

## Where authorization lives

Most query methods carry **no** `[Authorize]` attribute. Authorization is enforced inside the query handler, which calls `IAuthorizationService` against the `ClaimsPrincipal`. The GraphQL field is a passthrough.

When the GraphQL field itself must be gated (e.g. for a feature flag, or for cross-cutting policy that applies to the whole field set), apply `[Authorize(Policy = "<policy-name>")]`:

```csharp
[Authorize(Policy = "BooksRead")]
public async ValueTask<Book?> GetBookByIdAsync(...) { ... }
```

If you find yourself writing the same `[Authorize]` on every method, push it to the class:

```csharp
[QueryType]
[Authorize(Policy = "BooksRead")]
public sealed class BookQueries { ... }
```

## Multiple methods, multiple entities

A `<Entity>Queries` class is allowed to expose more than one method:

```csharp
[QueryType]
public sealed class AuthorQueries
{
    public async ValueTask<Author?> GetAuthorByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid authorId,
        CancellationToken ct)
        => await sender.QueryAsync(new GetAuthorByIdQuery(user, authorId), ct);
}
```

If the entity has multiple lookup forms (by id, by name, by external id), each becomes a separate method dispatching its own query:

```csharp
[QueryType]
public sealed class BookQueries
{
    public async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid id,
        CancellationToken ct)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);

    public async ValueTask<Book?> GetBookByIsbnAsync(
        ClaimsPrincipal user,
        ISender sender,
        string isbn,
        CancellationToken ct)
        => await sender.QueryAsync(new GetBookByIsbnQuery(user, isbn), ct);
}
```

## Paged top-level queries

Root-level paginated lists work the same way as field-level ones: `[UsePaging]` + `Connection<T>`.

```csharp
[QueryType]
public sealed class BookQueries
{
    [UsePaging]
    public async Task<Connection<Book>?> GetBooksAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid authorId,
        PagingArguments arguments,
        CancellationToken ct)
        => await (await sender.QueryAsync(new PageBooksByAuthorIdQuery(user, authorId, arguments), ct))
            .ToConnectionAsync();
}
```

The paging logic — query, sort, project, cursor — lives in the `PageBooksByAuthorIdQuery` handler. The GraphQL method only wires it up.

## Wrong vs right

**Wrong** — DbContext in the resolver:

```csharp
[QueryType]
public class BookQueries
{
    public async Task<Book?> GetBookByIdAsync(
        BookDbContext db,
        Guid id,                          // missing [ID<...>]
        CancellationToken ct)
    {
        return await db.Books             // DbContext in the resolver
            .FirstOrDefaultAsync(x => x.Id == id, ct);  // no auth, no DataLoader
    }
}
```

**Right** — dispatch the query through the mediator:

```csharp
[QueryType]
public class BookQueries
{
    public async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid id,
        CancellationToken ct)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
}
```

The wrong version compiles. It returns rows. It misses authorization, it bypasses batching, and it leaks change tracking. The right version is one line and gets all three for free.

## Gotchas

- **Forgetting `[ID<Entity>]`** — the field appears in the schema as a `String`, not an `ID`. Clients can't refetch and Relay caching breaks. Always tag id parameters.
- **Injecting a `DbContext` directly** — bypasses the Application layer. Don't. Inject `ISender` and dispatch the query.
- **Returning `ValueTask<Book>` (non-nullable) when the entity might not exist** — surface nullability honestly. `ValueTask<Book?>` for lookups that can miss.
- **Calling `sender.SendAsync` with an `IQuery<T>`** — compile error. Queries go through `QueryAsync`.
- **Returning `IQueryable<T>`** — only legal at root in Fusion subgraphs, never in a monolith. The monolith uses paged queries, not `IQueryable` exposure.
