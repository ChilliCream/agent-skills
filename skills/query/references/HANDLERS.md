# Query handler shapes

Worked examples of the canonical patterns beyond the single-entity get-by-id covered in the main skill. Every example is a Mocha mediator `IQuery<T>` + `IQueryHandler<T, R>` pair.

## List by parent

Returns a flat `IReadOnlyList<T>?`. The handler authorizes the **parent**, not each child — children inherit the parent's read permission.

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;

public sealed record GetBooksByAuthorIdQuery(ClaimsPrincipal User, Guid AuthorId)
    : IQuery<IReadOnlyList<Book>?>;

public sealed class GetBooksByAuthorIdQueryHandler(
    IAuthorBatchingContext authors,
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBooksByAuthorIdQuery, IReadOnlyList<Book>?>
{
    public async ValueTask<IReadOnlyList<Book>?> HandleAsync(
        GetBooksByAuthorIdQuery query,
        CancellationToken ct)
    {
        var (user, authorId) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        // Authorize the parent — children inherit read permission
        var author = await authors.AuthorById.LoadAsync(authorId, ct);
        if (author is null)
        {
            return null;
        }

        var auth = await authorization.AuthorizeAsync(user, author, "Books.Read");
        if (!auth.Succeeded)
        {
            return null;
        }

        return await books.BooksByAuthorId.LoadAsync(authorId, ct);
    }
}
```

Why authorize the parent and not each child: the read-permission policy on `Book` typically resolves up to `Author` anyway. One auth call beats N, and you never partially leak — either the whole list is visible or it's not.

## Paged list by parent

Returns `Page<T>?` (HotChocolate's connection model). The paged DataLoader takes the paging arguments through `.With(arguments)`.

```csharp
using System.Security.Claims;
using HotChocolate.Pagination;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;

public sealed record PageBooksByAuthorIdQuery(
    ClaimsPrincipal User,
    Guid AuthorId,
    PagingArguments Paging) : IQuery<Page<Book>?>;

public sealed class PageBooksByAuthorIdQueryHandler(
    IAuthorBatchingContext authors,
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<PageBooksByAuthorIdQuery, Page<Book>?>
{
    public async ValueTask<Page<Book>?> HandleAsync(
        PageBooksByAuthorIdQuery query,
        CancellationToken ct)
    {
        var (user, authorId, paging) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var author = await authors.AuthorById.LoadAsync(authorId, ct);
        if (author is null)
        {
            return null;
        }

        var auth = await authorization.AuthorizeAsync(user, author, "Books.Read");
        if (!auth.Succeeded)
        {
            return null;
        }

        return await books
            .PageBooksByAuthorId.With(paging)
            .LoadAsync(authorId, ct);
    }
}
```

In the GraphQL wrapper, this becomes:

```csharp
public Task<Page<Book>?> GetBooksAsync(
    ClaimsPrincipal user,
    ISender sender,
    [ID(nameof(Author))] Guid authorId,
    PagingArguments paging,
    CancellationToken ct)
{
    return sender.QueryAsync(new PageBooksByAuthorIdQuery(user, authorId, paging), ct).AsTask();
}
```

> Do **not** add `[UsePaging]` on top of a method that already returns `Page<T>`. The `Page<T>` type is HotChocolate's own connection model; double-wrapping it produces a malformed schema. See [GRAPHQL-WRAPPER.md](GRAPHQL-WRAPPER.md).

## Tenant-scoped query

Multi-tenant apps often need the active tenant as part of the request shape so the handler can constrain the load *and* the authorization policy can see it. Pass the tenant id explicitly — don't pull it from ambient state inside the handler.

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;

public sealed record GetBookByIdInTenantQuery(ClaimsPrincipal User, Guid TenantId, Guid Id)
    : IQuery<Book?>;

public sealed class GetBookByIdInTenantQueryHandler(
    ITenantBatchingContext tenants,
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBookByIdInTenantQuery, Book?>
{
    public async ValueTask<Book?> HandleAsync(
        GetBookByIdInTenantQuery query,
        CancellationToken ct)
    {
        var (user, tenantId, id) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var tenant = await tenants.TenantById.LoadAsync(tenantId, ct);
        if (tenant is null)
        {
            return null;
        }

        var book = await books.BookById.LoadAsync(id, ct);
        if (book is null || book.TenantId != tenantId)
        {
            return null;
        }

        // The "Books.Read" handler can read the tenant off the resource (or check
        // the user's tenant claim) and reject cross-tenant access.
        var auth = await authorization.AuthorizeAsync(user, book, "Books.Read");
        if (!auth.Succeeded)
        {
            return null;
        }

        return book;
    }
}
```

Notice: load both the tenant and the book, cross-check that the book actually belongs to the tenant (defense in depth — never trust a single source), then authorize.

## Lookup by non-id key

Same shape — replace the id parameter with whatever key you're looking up by.

```csharp
public sealed record GetAuthorByNameQuery(ClaimsPrincipal User, string Name) : IQuery<Author?>;

public sealed class GetAuthorByNameQueryHandler(
    IAuthorBatchingContext authors,
    IAuthorizationService authorization)
    : IQueryHandler<GetAuthorByNameQuery, Author?>
{
    public async ValueTask<Author?> HandleAsync(
        GetAuthorByNameQuery query,
        CancellationToken ct)
    {
        var (user, name) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var author = await authors.AuthorByName.LoadAsync(name, ct);
        if (author is null)
        {
            return null;
        }

        var auth = await authorization.AuthorizeAsync(user, author, "Authors.Read");
        if (!auth.Succeeded)
        {
            return null;
        }

        return author;
    }
}
```

The DataLoader interface (`AuthorByName`) must be configured to key on `string` — see the `dataloader` skill.

## Post-mutation re-read (called from a mutation resolver)

After a command, a mutation can dispatch a query through the same `ISender` to return a freshly-read view of the entity. This pattern only makes sense when (a) the read enforces a different permission than the write, or (b) the response should go through DataLoaders for downstream cache sharing.

```csharp
[MutationType]
public class BookMutations
{
    [Authorize]
    public async Task<Book> CreateBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        CreateBookInput input,
        CancellationToken cancellationToken)
    {
        // Command — uses "Books.Write"
        var bookId = await sender.SendAsync(
            new CreateBookCommand(user, input),
            cancellationToken);

        // Query — uses "Books.Read", shares the DataLoader cache
        return (await sender.QueryAsync(
            new GetBookByIdQuery(user, bookId),
            cancellationToken))!;
    }
}
```

`SendAsync` and `QueryAsync` are distinct methods on `ISender` — picking the wrong one is a compile error.

The `!` on the result is intentional: in the post-mutation case you've just authorized + persisted, so the query handler returning null means a permission inversion between write and read (which is a bug). Let the NRE surface — don't paper over it.

## Common pitfalls

- **`Task<T?>` instead of `ValueTask<T?>`** — handler signature won't satisfy `IQueryHandler<,>`. Always `ValueTask`.
- **`Handle` instead of `HandleAsync`** — same. Mocha's interface declares `HandleAsync`.
- **Default value on `CancellationToken`** — the interface signature has none. Adding `= default` works at the call site but breaks consistency with the interface contract.
- **Returning `null` from `IQuery<T>` where `T` is non-nullable** — Mocha rejects `null` responses at runtime with an `InvalidOperationException`. Declare `IQuery<T?>` when null is meaningful.
- **Forgetting to authorize the parent for list queries** — easy to leak. List handlers must do the parent check.
- **Conflating `SendAsync` and `QueryAsync`** — they're separate methods. The compiler will tell you, but don't reach for `.SendAsync(IQuery<T>)` out of MediatR muscle memory.
