# GraphQL wrapper patterns for application queries

The `[QueryType]` class is intentionally thin: it adapts GraphQL parameters (Relay node ids, paging args, projection hints) to a Mocha `IQuery<T>` and dispatches via `ISender.QueryAsync`. Business logic lives in the query handler.

## Single-entity get-by-id

```csharp
using System.Security.Claims;
using HotChocolate.Types.Relay;
using Mocha.Mediator;

namespace MyApp.GraphQL.Books;

[QueryType]
public class BookQueries
{
    public Task<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Book))] Guid id,
        CancellationToken cancellationToken)
    {
        return sender
            .QueryAsync(new GetBookByIdQuery(user, id), cancellationToken)
            .AsTask();
    }
}
```

- `[ID(nameof(Book))]` decodes a Relay node id into a `Guid` at the boundary.
- `sender` is parameter-injected (per-request scope), not constructor-injected.
- Method body is a single `return` — never add validation, error mapping, or transforms here.
- `.AsTask()` converts the Mocha `ValueTask<Book?>` to `Task<Book?>` for the GraphQL middleware. Drop it when the surrounding signature accepts `ValueTask<T>` directly.

## Get-by-id when the type is `static`

Some entities use a static `[QueryType]` class. Behavior is identical:

```csharp
[QueryType]
public static class AuthorQueries
{
    public static Task<Author?> GetAuthorByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Author))] Guid id,
        CancellationToken cancellationToken) =>
        sender.QueryAsync(new GetAuthorByIdQuery(user, id), cancellationToken).AsTask();
}
```

Static or instance is a per-entity style choice. Don't refactor between them on the side; match the surrounding code.

## Paginated lists

When the query handler returns `Page<T>?`, the GraphQL wrapper just forwards `PagingArguments`:

```csharp
[QueryType]
public class AuthorQueries
{
    public Task<Page<Book>?> GetBooksAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Author))] Guid authorId,
        PagingArguments paging,
        CancellationToken cancellationToken)
    {
        return sender
            .QueryAsync(new PageBooksByAuthorIdQuery(user, authorId, paging), cancellationToken)
            .AsTask();
    }
}
```

The handler composes the paged DataLoader internally — for example:

```csharp
return await books
    .PageBooksByAuthorId.With(paging)
    .LoadAsync(query.AuthorId, cancellationToken);
```

Do **not** add `[UsePaging]` on top of a method that already returns `Page<T>`. The `Page<T>` type is HotChocolate's own connection model; double-wrapping it produces a malformed schema.

## When the entity is field-resolved on a parent

Sometimes the "query" lives as a field on another type, not at the root. Dispatch via `ISender.QueryAsync` from the field resolver:

```csharp
public sealed class AuthorNode : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Field("books").ResolveWith<AuthorNodeResolvers>(
            r => r.GetBooks(default!, default!, default!, default!, default!));
    }
}

public sealed class AuthorNodeResolvers
{
    public Task<Page<Book>?> GetBooks(
        [Parent] Author author,
        ClaimsPrincipal user,
        ISender sender,
        PagingArguments paging,
        CancellationToken ct)
    {
        return sender
            .QueryAsync(new PageBooksByAuthorIdQuery(user, author.Id, paging), ct)
            .AsTask();
    }
}
```

Resolver still delegates to a query handler. Don't reimplement the auth/DataLoader flow inside the resolver.

## Node resolver — `[NodeResolver]`

Every entity exposed as a Relay node has a `[NodeResolver]` method on its `<Entity>Type` class. The simplest form dispatches the `GetByIdQuery` directly:

```csharp
[NodeResolver]
public static Task<Book?> GetBookAsync(
    Guid id,
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken cancellationToken)
    => sender
        .QueryAsync(new GetBookByIdQuery(user, id), cancellationToken)
        .AsTask();
```

The handler enforces authorization — nothing extra needed at the GraphQL boundary.

## Things that belong in the handler, not the wrapper

- Authorization. The wrapper never calls `IAuthorizationService`.
- Existence checks. The wrapper never calls `LoadAsync`.
- Validation. Inputs are validated inside `HandleAsync` (or by FluentValidation on the command path).
- Error throwing. The wrapper never throws — if the handler returns null, the field is null.

If you find yourself adding `if (result is null) throw ...` in the wrapper, you have GraphQL pretending to be a command. Stop and rethink: the read path's contract is "null means you can't see it".

## Things that belong in the wrapper, not the handler

- Relay node id encoding/decoding (`[ID(nameof(...))]`).
- Authorization-free shaping decorators (`[UseProjection]`, `[UseSorting]`, `[UseFiltering]`) when applied to an `IQueryable<T>` resolver — but prefer the handler-returns-`Page<T>` model.
- GraphQL-only naming overrides (`[GraphQLName("...")]`).
- `.AsTask()` adapters between Mocha's `ValueTask` and HotChocolate's `Task` middleware signatures.

## Common mistakes

- **Calling `sender.SendAsync(new GetByIdQuery(...))`.** Queries go through `QueryAsync`. `SendAsync(IQuery<T>)` doesn't compile against `ISender`.
- **Constructor-injecting `ISender` into the `[QueryType]` class.** HotChocolate cannot scope it per-request that way; use parameter injection.
- **Catching exceptions in the wrapper and returning null.** The handler is the only thing allowed to decide "this is a null". If something throws out of `HandleAsync`, it's a bug, not an auth event — let it surface.
- **Adding pagination logic in the wrapper.** If the wrapper builds a query, you've written the read path twice.
- **Calling a stand-alone `Get<Entity>ById` class with `ExecuteAsync`.** That's the pre-Mocha pattern. Queries are now mediator messages — dispatch through `ISender.QueryAsync`.
