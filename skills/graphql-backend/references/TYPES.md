# TYPES — `[ObjectType<T>]`

The `<Entity>Type` class is where GraphQL meets the domain entity. The `[ObjectType<T>]` source generator emits the binding; you author resolvers as `public static` methods on a `public static partial class`.

## Anatomy of an `<Entity>Type` class

```csharp
[ObjectType<Book>]
public static partial class BookType
{
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(t => t.AuthorId);
        descriptor.Ignore(x => x.Reviews);
    }

    [NodeResolver]
    public static async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        Guid id,
        CancellationToken ct)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);

    [BindMember(nameof(Book.AuthorId))]
    public static async ValueTask<Author?> GetAuthorAsync(
        [Parent] Book book,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
        => await sender.QueryAsync(new GetAuthorByIdQuery(user, book.AuthorId), ct);

    [UsePaging]
    public static async Task<Connection<Review>?> GetReviewsAsync(
        [Parent] Book book,
        ClaimsPrincipal user,
        ISender sender,
        PagingArguments arguments,
        CancellationToken ct)
        => await (await sender.QueryAsync(new PageReviewsByBookIdQuery(user, book.Id, arguments), ct))
            .ToConnectionAsync();
}
```

Five things are happening:

1. `[ObjectType<Book>]` — the generator binds this class to the `Book` domain entity. Every public property on `Book` becomes a GraphQL field unless `Configure` ignores it.
2. `static partial void Configure(...)` — the seam for descriptor-level customization. Use it for `Ignore`, `Field(...).Type(...)`, `Authorize`, etc. The generator wires it.
3. `[NodeResolver]` — declares the Relay refetch resolver. Mandatory for every type that appears as a node. See [NODE-RESOLVER](NODE-RESOLVER.md).
4. `[BindMember(nameof(Book.AuthorId))]` — replaces the auto-bound `authorId: ID!` field with a typed `author: Author` resolver. Cleaner schema, single source of truth.
5. `[UsePaging]` + `Connection<T>` — adds a Relay cursor connection. The resolver dispatches the paged query and adapts the `Page<T>` result via `.ToConnectionAsync()`.

## `Configure(IObjectTypeDescriptor<T> descriptor)`

The `partial` method runs at schema build time. Use it for things the attribute model can't express:

```csharp
static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
{
    descriptor.Ignore(x => x.AuthorId);
}
```

Common uses:

- `descriptor.Ignore(x => x.RawForeignKey)` — for FK columns replaced by typed resolvers.
- `descriptor.Ignore(x => x.NavigationProperty)` — for unloaded EF navigations.
- `descriptor.Field(x => x.SomeProperty).Type<NonNullType<StringType>>()` — to refine a property's type.
- `descriptor.Authorize("<policy-name>")` — to gate the whole type.

If `Configure` has nothing to do, omit it. The generator still works.

## Resolver methods

Resolvers are `public static` methods. HotChocolate injects parameters by attribute or by service location.

```csharp
public static async ValueTask<Author?> GetAuthorAsync(
    [Parent] Book book,                       // the parent entity
    ClaimsPrincipal user,                     // service from DI
    ISender sender,                           // the Mocha mediator dispatcher
    CancellationToken ct)                     // request-scoped
    => await sender.QueryAsync(new GetAuthorByIdQuery(user, book.AuthorId), ct);
```

Naming rules:

- Method name `Get<FieldName>` → GraphQL field `<fieldName>` (camelCased). HotChocolate strips the `Get` prefix and the `Async` suffix.
- A resolver named `GetAuthorAsync` on `BookType` adds an `author: Author` field to the `Book` GraphQL type.
- Use `[BindMember(nameof(Entity.Property))]` when you need the resolver to *replace* an existing property of the same camelCased name. Without `[BindMember]`, you risk duplicate fields or ambiguous names.

## `[Parent]` injection

`[Parent] T parent` gives the resolver access to the parent entity. HotChocolate fills it from the resolver tree — at root, the parent is whatever the query returned; at depth, it's the result of the field above.

```csharp
public static async ValueTask<IReadOnlyList<Chapter>> GetChaptersAsync(
    [Parent] Book book,
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken ct)
    => await sender.QueryAsync(new GetChaptersByBookIdQuery(user, book.Id), ct);
```

No `[Parent]` = no access to the parent. Forgetting it on a non-root resolver compiles but produces a null-ref or empty result at runtime.

## `[BindMember]`

`[BindMember]` ties a resolver to an existing property on the entity, replacing it.

```csharp
[BindMember(nameof(Book.AuthorId))]
public static async ValueTask<Author?> GetAuthorAsync(
    [Parent] Book book,
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken ct)
    => await sender.QueryAsync(new GetAuthorByIdQuery(user, book.AuthorId), ct);
```

Why: the `Book` domain entity exposes `AuthorId: Guid`. The GraphQL schema should expose `author: Author`. `[BindMember(nameof(Book.AuthorId))]` says "this resolver replaces the field named after `AuthorId`" — so the property name in the schema becomes `author`, not `authorId`, and the type becomes `Author`, not `ID`.

Pairing `[BindMember]` with `descriptor.Ignore(x => x.AuthorId)` is **wrong** — `BindMember` already replaces it. Use one or the other.

## `[UsePaging]` and `Connection<T>`

Collection resolvers return `Connection<T>` and are annotated `[UsePaging]`. The paged query handler returns `Page<T>`; `.ToConnectionAsync()` adapts it.

```csharp
[UsePaging]
public static async Task<Connection<Review>?> GetReviewsAsync(
    [Parent] Book book,
    ClaimsPrincipal user,
    ISender sender,
    PagingArguments arguments,
    CancellationToken ct)
{
    var page = await sender.QueryAsync(new PageReviewsByBookIdQuery(user, book.Id, arguments), ct);
    return await page.ToConnectionAsync();
}
```

`[UsePaging]` common options:

- `ConnectionName = "BookReviewCollection"` — overrides the auto-derived connection name. Use when two collections of the same type would otherwise collide on the schema.
- `AllowBackwardPagination = false` — when the underlying store cannot page backward efficiently.
- `IncludeTotalCount = true` — when the UI needs the total. Beware: counting is an extra query.

`PagingArguments` is the standardized input (`first`, `after`, `last`, `before`). Always accept it from the resolver and pass it through to the paged query.

## `[ExtendObjectType<T>]`

Cross-cutting fields that don't belong with the main type live in `<Entity>/Extensions/<Entity>Extensions.cs`:

```csharp
[ExtendObjectType<Book>]
public static class BookExtensions
{
    [UsePaging(IncludeTotalCount = false, AllowBackwardPagination = false, ConnectionName = "BookAnnotation")]
    [Authorize(Policy = "AnnotationsRead")]
    public static async Task<Connection<Annotation>> GetAnnotationsAsync(
        [Parent] Book book,
        ClaimsPrincipal user,
        ISender sender,
        PagingArguments arguments,
        CancellationToken ct)
        => await (await sender.QueryAsync(new PageAnnotationsByBookIdQuery(user, book.Id, arguments), ct))
            .ToConnectionAsync();
}
```

Use cases:

- A field that depends on a feature/module that lives in another project (cross-project schema additions).
- A field that needs a different `Authorize` policy than the rest of the type.
- Optional features behind a feature flag.

Don't reach for `[ExtendObjectType<T>]` when the field could live on `<Entity>Type` — split only when there's a real reason.

## `IResolverContext`

When a resolver needs the GraphQL execution context (path, selection set, authorization handler), inject `IResolverContext`:

```csharp
[NodeResolver]
public static async ValueTask<Book?> GetNodeAsync(
    Guid id,
    ClaimsPrincipal user,
    ISender sender,
    [Service] IAuthorizationHandler handler,
    IResolverContext context,
    CancellationToken ct)
{
    var result = await handler.AuthorizeAsync(
        (IMiddlewareContext)context,
        _booksRead,
        ct);

    if (result != AuthorizeResult.Allowed)
    {
        throw new GraphQLException(
            ErrorBuilder.New()
                .SetMessage("...")
                .SetCode(ErrorCodes.Authentication.NotAuthorized)
                .SetPath(context.Path)
                .Build());
    }

    return await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
}
```

`IResolverContext` is heavy — only inject it when you need it. For simple resolvers, prefer `[Parent]` + `ClaimsPrincipal` + `ISender`.

## Gotchas

- **Forgetting `static partial`** — the generator silently doesn't emit if the class isn't `partial`. The schema builds, but your type isn't registered. Always declare `public static partial class <Entity>Type`.
- **`Configure` not being `static partial void`** — same failure mode. The exact signature is `static partial void Configure(IObjectTypeDescriptor<T> descriptor)`.
- **Resolvers that aren't `public static`** — instance methods compile but the generator skips them. The field never appears in the schema.
- **Returning `IEnumerable<T>` from a `[UsePaging]` field** — pagination breaks. The return must be `Connection<T>` or `Page<T>` (via `.ToConnectionAsync()`).
- **Hardcoding the GraphQL field name** — let the generator derive it from the method name. Use `[BindMember]` if you need to replace an existing property; use `descriptor.Field(...).Name(...)` in `Configure` only as a last resort.
