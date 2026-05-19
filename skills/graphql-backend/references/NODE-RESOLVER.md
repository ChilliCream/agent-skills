# NODE-RESOLVER — `[NodeResolver]`

Relay's Global Object Identification requires every entity exposed in the graph to be refetchable by global ID via the root `node(id: ID!)` field. HotChocolate implements this by routing the global ID to a `[NodeResolver]` method on the entity's GraphQL type.

**Every entity in the graph MUST have a `[NodeResolver]`.** Schema construction validates this — a missing node resolver throws at startup.

## What a global ID is

A Relay global ID is base64(`<TypeName>:<rawId>`). HotChocolate's middleware:

1. Decodes the base64.
2. Looks up the type (`Book`, `Author`, `Review`, ...).
3. Routes the raw id portion to that type's `[NodeResolver]`.
4. Returns the loaded entity.

This is how `node(id: "Qm9vazoyODk3Mw==...")` works for any entity. It's also how Relay clients refetch cached entities by ID after mutations.

## Preferred form — dispatch the query through the mediator

90% of node resolvers are a one-line dispatch to the matching `IQuery<T>`:

```csharp
[ObjectType<Book>]
public static partial class BookType
{
    [NodeResolver]
    public static async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        Guid id,
        CancellationToken ct)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
}
```

Rules for the preferred form:

- One of the parameters is `Guid id` (HotChocolate decodes the global ID for you — no `[ID]` attribute needed).
- Inject `ClaimsPrincipal`, `ISender`, and `CancellationToken`.
- The body is a single call to `sender.QueryAsync(new Get<Entity>ByIdQuery(user, id), ct)`. The query handler enforces authorization.
- Return `ValueTask<Entity?>` — nullable, because the entity might not exist or the caller might not be allowed to see it.

This works because:

- The query handler already calls `IAuthorizationService` against the `ClaimsPrincipal`. If the user can't see this entity, the query returns `null` and the node lookup naturally fails.
- DataLoaders inside the handler de-duplicate concurrent loads — Relay refetches and field-level resolvers share the same batch.

## Auth-handler form — when GraphQL-level auth is required

Use the auth-handler form only when the GraphQL field needs a directive-style policy check **before** the query runs:

```csharp
[ObjectType<Book>]
public static partial class BookType
{
    private static readonly AuthorizeDirective _booksRead =
        new("BooksRead");

    [NodeResolver]
    public static async ValueTask<Book?> GetNodeAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        [Service] IAuthorizationHandler handler,
        IResolverContext context,
        CancellationToken ct)
    {
        AuthorizeResult result = await handler
            .AuthorizeAsync((IMiddlewareContext)context, _booksRead, ct);

        if (result != AuthorizeResult.Allowed)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The current user is not authorized to access this resource.")
                    .SetCode(ErrorCodes.Authentication.NotAuthorized)
                    .SetPath(context.Path)
                    .Build());
        }

        return await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
    }
}
```

Why this form exists: `Book` has a policy (`BooksRead`) that gates entire categories of fields. Running that check at the node level prevents a partial load (entity returned, but downstream fields would have been denied anyway).

Rules:

- Use a static `AuthorizeDirective` instance — building one per request is wasteful.
- Cast `IResolverContext` to `IMiddlewareContext` when calling `AuthorizeAsync` (it's the contract).
- On denial, throw a `GraphQLException` with code `ErrorCodes.Authentication.NotAuthorized`. Don't return `null` — that signals "not found" semantically, which is wrong for "not authorized".

**Default to the preferred form.** Reach for the auth-handler form only when:

- A directive-based policy applies (`AuthorizeDirective`) and must run before any query work.
- The check is needed at the field boundary, not just inside the handler.

If you're tempted to use the auth-handler form because the query handler doesn't authorize — the fix is in the query handler, not here. See [`query`](../../query/SKILL.md).

## ID parameter forms

| Form | Use |
|---|---|
| `Guid id` | New code. HotChocolate decodes the global ID into the raw type. |
| `[ID] string id` | Older code. The parameter receives the **encoded** global ID; you must `Guid.Parse(id)` after manual decoding. Avoid for new code. |
| `[ID<Entity>] Guid id` | Wrong here — that form is for arguments on queries/mutations, not for node resolvers. The node resolver's ID is implicit because the type is already known via `[NodeResolver]`. |

## Where the node resolver goes

On the entity's `<Entity>Type` class. Not on a query class. Not on a separate file.

```
<Entity>/
  Types/
    <Entity>Type.cs              <-- node resolver lives here
```

One `[NodeResolver]` per entity. If you have two `[NodeResolver]`s on the same type, schema construction fails.

## Fusion subgraphs — pair with `[Lookup]`

In Fusion source schemas, `[NodeResolver]` is paired with `[Lookup]`:

```csharp
[QueryType]
public static class Query
{
    [NodeResolver]
    [Lookup]
    public static async ValueTask<Product?> GetProductByIdAsync(
        int id,
        ProductByIdDataLoader productById,
        CancellationToken ct)
        => await productById.LoadAsync(id, ct);
}
```

`[Lookup]` is the Fusion-level marker: it tells the composition layer that this method is the canonical lookup for `Product` by `id`. Multiple `[Lookup]` methods per entity are allowed when there are multiple lookup keys (e.g. `GetProductByIdAsync` and `GetProductBySkuAsync`).

This only applies in subgraph projects. In a monolithic GraphQL server there is no Fusion composition, so `[Lookup]` is not used.

## Wrong vs right

**Wrong — node resolver missing:**

```csharp
[ObjectType<Book>]
public static partial class BookType
{
    // no [NodeResolver] — schema build fails or, worse, the type is excluded from `node`.
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor) { ... }
}
```

**Wrong — node resolver loading directly from a DataLoader/DbContext:**

```csharp
[NodeResolver]
public static async ValueTask<Book?> GetBookByIdAsync(
    Guid id,
    IBookByIdDataLoader bookById,            // skips the handler's auth check
    CancellationToken ct)
    => await bookById.LoadAsync(id, ct);
```

This loads the entity but bypasses authorization. Any caller can refetch any global ID and see the data.

**Wrong — node resolver with business logic:**

```csharp
[NodeResolver]
public static async ValueTask<Book?> GetBookByIdAsync(
    Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct)
{
    var book = await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
    if (book?.IsArchived == true)         // business logic in the resolver
        return null;
    return book;
}
```

The "is archived" check belongs in the query handler or the authorization service. The node resolver is a passthrough.

**Right — preferred form:**

```csharp
[NodeResolver]
public static async ValueTask<Book?> GetBookByIdAsync(
    ClaimsPrincipal user,
    ISender sender,
    Guid id,
    CancellationToken ct)
    => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
```

## Gotchas

- **Forgetting `[NodeResolver]` entirely.** Schema construction surfaces this at startup. Add it before merging.
- **Two `[NodeResolver]` methods on the same type.** Schema construction fails. There's only one canonical lookup per entity.
- **Returning the wrong type.** The return type must be the entity type the class is bound to (`ValueTask<Book?>` on `[ObjectType<Book>]`). Returning a subset or DTO breaks Relay refetching.
- **Skipping the `ClaimsPrincipal` injection in the auth-handler form.** The query still needs the principal to authorize. The auth handler is *additional*, not *instead-of*.
- **Using `[ID<Entity>] Guid id` on a node resolver.** That's for query/mutation arguments. Node resolvers take bare `Guid id` (the global ID is already decoded for you).
