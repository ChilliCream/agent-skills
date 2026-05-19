---
name: graphql-backend
description: HotChocolate v15/v16 best practices for a GraphQL backend — queries, mutations, types, node resolvers, error unions, mutation conventions, Mocha mediator integration. Fire whenever the user mentions HotChocolate, "[QueryType]", "[MutationType]", "[ObjectType<T>]", "[NodeResolver]", "[ID<", "[Error<", "[Lookup]", "[UsePaging]", "GraphQL backend", "GraphQL API", "Relay node", "mutation conventions", or "Banana Cake Pop", and whenever editing any file under `**/GraphQL/<Entity>/Types/*.cs`, `**/GraphQL/<Entity>/Operations/*.cs`, `**/GraphQL/<Entity>/Extensions/*.cs`, or any file matching `*Type.cs`, `*Queries.cs`, `*Mutations.cs` in a HotChocolate project. Bias toward firing on any GraphQL-layer edit.
---

# graphql-backend

This is the API layer of a HotChocolate server. It exposes the Application layer over GraphQL using HotChocolate's source generators (`[ObjectType<T>]`, `[QueryType]`, `[MutationType]`, `[NodeResolver]`).

The single most important principle: **the GraphQL layer is a thin wrapper.** Resolvers translate arguments, dispatch to the Application layer via the Mocha mediator (`ISender.SendAsync` for commands, `ISender.QueryAsync` for queries), and return the result. No business logic. No validation beyond what the type system already enforces. No EF Core. If you find yourself writing `if`/`else` over domain state in a resolver, the work belongs in a command or query handler — see [`command`](../command/SKILL.md) and [`query`](../query/SKILL.md).

## Project layout

Every entity mirrors the Application layer:

```
src/MyApp.GraphQL/
  Books/
    Operations/
      BookQueries.cs        # [QueryType]
      BookMutations.cs      # [MutationType]
    Types/
      BookType.cs           # [ObjectType<T>] + [NodeResolver] + resolvers
    Extensions/             # optional [ExtendObjectType<T>]
    Inputs/                 # optional — only for shared/oneOf inputs
```

One folder per entity, mirroring `MyApp.Application/Books/`. An agent that has seen one entity has seen them all.

## Types — `[ObjectType<T>]`

The entity's GraphQL type is a `static partial class` paired with the domain entity via the `[ObjectType<T>]` source generator. Field resolvers live as `public static` methods on that class.

```csharp
[ObjectType<Book>]
public static partial class BookType
{
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(t => t.AuthorId);
    }

    [NodeResolver]
    public static async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        Guid id,
        CancellationToken ct)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);

    public static async ValueTask<Author?> GetAuthorAsync(
        [Parent] Book book,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
        => await sender.QueryAsync(new GetAuthorByIdQuery(user, book.AuthorId), ct);
}
```

Rules:

- `public static partial class <Entity>Type` — the source generator emits the rest.
- `static partial void Configure(IObjectTypeDescriptor<T> descriptor)` — use it to `Ignore` raw IDs and navigation properties, since they're replaced by typed resolvers.
- Resolvers are `public static` methods. Inject `[Parent] T parent`, services, the user principal, the mediator surface (`ISender`), and `CancellationToken` — never the DbContext.
- Use `[BindMember(nameof(Entity.Property))]` to attach a resolver to an existing property name.
- Use `[UsePaging]` + `Connection<T>` for collection fields, dispatching the paged `IQuery<Page<T>?>` via `sender.QueryAsync(...)` and converting with `.ToConnectionAsync()`.

Deep dive: [TYPES](references/TYPES.md).

## Queries — `[QueryType]`

A query class is a `[QueryType]`-annotated class with one method per top-level read. Each method translates GraphQL arguments to a Mocha `IQuery<T>`, dispatches via `ISender.QueryAsync`, and returns the response.

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

- One `<Entity>Queries.cs` per entity. Multiple `[QueryType]` classes contribute to the same root `Query`.
- Inject `ISender` (not `IMediator` unless the same method also publishes notifications). Never a DbContext or a DataLoader directly.
- Dispatch through `sender.QueryAsync(new <X>Query(...), ct)` — note `QueryAsync`, **not** `SendAsync`. Calling `SendAsync` with an `IQuery<T>` is a compile error.
- ID arguments use `[ID<Entity>] Guid id` so HotChocolate decodes the Relay global ID. See [QUERIES](references/QUERIES.md).
- Return `ValueTask<T>` from query methods — that matches the Mocha surface and avoids unnecessary `.AsTask()` adapters.
- No `[Authorize]` on most queries — authorization lives in the query handler via `IAuthorizationService`. Apply `[Authorize(Policy = ...)]` only when the GraphQL field itself needs gating beyond what the handler does.

Deep dive: [QUERIES](references/QUERIES.md).

## Mutations — `[MutationType]`

A mutation class is `[MutationType]`-annotated. Each method translates GraphQL arguments to a Mocha `ICommand` / `ICommand<T>`, dispatches via `ISender.SendAsync`, and returns the entity (mutation conventions wrap the return in a payload).

```csharp
using Mocha.Mediator;

[MutationType]
public class BookMutations
{
    [Authorize]
    [Error<AuthorNotFoundError>]
    [Error<UnauthorizedOperation>]
    public async ValueTask<Book> UpdateBookTitleAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid bookId,
        string title,
        CancellationToken ct)
        => await sender.SendAsync(new UpdateBookTitleCommand(user, bookId, title), ct);
}
```

Rules:

- Always inject `ISender`, `ClaimsPrincipal`, and `CancellationToken`. Pass the principal into the command so the Application layer enforces authorization.
- Use `sender.SendAsync(command, ct)` for commands. Note `SendAsync`, not `Send` — Mocha's surface is `Async`/`ValueTask`-based.
- Declare domain errors with `[Error<TError>]` (the generic form). Each `TError` must implement `IError` or derive from `Exception`. Never use `[Error(typeof(T))]` in new code — the generic form is the convention.
- Apply `[Authorize]` for the common case; specific policies (`[Authorize(Policy = "DocumentsWrite")]`) when needed. Authorization happens primarily in the command handler — `[Authorize]` is a coarse gate.
- **Never hand-roll an input type.** HotChocolate's mutation conventions wrap method parameters into a generated `<Mutation>Input` and the return value into a generated `<Mutation>Payload`. Hand-rolling defeats the conventions and breaks the GraphQL contract. See [MUTATION-CONVENTIONS](references/MUTATION-CONVENTIONS.md).

When adding a mutation, the real work happens in [`command`](../command/SKILL.md); the GraphQL layer is the thin wrapper described here.

Deep dive: [MUTATIONS](references/MUTATIONS.md).

## Node resolver — `[NodeResolver]`

Every entity exposed as a Relay node MUST have a `[NodeResolver]` method on its `<Entity>Type` class. This is how Relay resolves `node(id: ID!)` and refetches a cached entity by global ID.

**Preferred form** — dispatch the `Get<Entity>ByIdQuery` through the mediator:

```csharp
[NodeResolver]
public static async ValueTask<Book?> GetBookByIdAsync(
    ClaimsPrincipal user,
    ISender sender,
    Guid id,
    CancellationToken ct)
    => await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
```

The query handler already enforces authorization through `IAuthorizationService`; nothing else is needed.

**Auth-handler form** — only when the GraphQL field needs an extra gate beyond what the query enforces (e.g. a directive-based policy that runs at the GraphQL boundary):

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
```

Default to the preferred form. Reach for the auth-handler form only when there's a directive policy that must run before the query is dispatched.

Deep dive: [NODE-RESOLVER](references/NODE-RESOLVER.md).

## ID parameters

| Where | Form | Why |
|---|---|---|
| Query / mutation argument | `[ID<Entity>] Guid id` | Decodes the Relay global ID into a domain `Guid`. The generic form ties the ID type to the entity type. |
| Same, string form | `[ID(nameof(Entity))] Guid id` | Equivalent. Use either; be consistent within a file. |
| `[NodeResolver]` parameter | `Guid id` (no attribute) | HotChocolate decodes the global ID before invoking the resolver. The parameter is the raw entity id. Older code sometimes uses `[ID] string id` — both work; raw `Guid` is preferred for new code. |
| Property on an input/error type | `[ID<Entity>] public Guid EntityId { get; }` | Re-encodes the id when serialized back to the client. |

```csharp
public sealed class AuthorNotFoundError(Guid authorId) : Exception("Author was not found")
{
    [ID<Author>]
    public Guid AuthorId { get; } = authorId;
}
```

The error type is project-defined and lives in the Application layer next to the command/query handler that throws it.

## Lookup — Fusion subgraphs only

`[Lookup]` is the Fusion source-schema pairing for `[NodeResolver]`. It marks a query method as a key-based lookup so the composition layer knows how to refetch an entity across subgraphs. It only applies in **source schemas** (Fusion subgraphs), not in a monolithic GraphQL server.

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

    [Lookup]
    public static async ValueTask<Product?> GetProductBySkuAsync(
        string sku,
        ProductBySkuDataLoader productBySku,
        CancellationToken ct)
        => await productBySku.LoadAsync(sku, ct);
}
```

In a monolithic server (not Fusion-composed) you will not write `[Lookup]`. In a subgraph project, every entity that may be looked up across subgraphs needs a `[Lookup]` query method — by-id paired with `[NodeResolver]`, plus an extra `[Lookup]` for each alternate key. Don't load directly from the DbContext; route through a DataLoader.

## Errors — `[Error<T>]`

Errors are declarative. Each mutation lists the union of domain errors it may produce; HotChocolate generates a GraphQL union and maps thrown exceptions to it.

```csharp
[Error<AuthorNotFoundError>]
[Error<UnauthorizedOperation>]
[Error<BookDeletionFailedError>]
public async ValueTask<Book> DeleteBookByIdAsync(...) { ... }
```

Rules:

- Always use the generic form `[Error<T>]`. The legacy `[Error(typeof(T))]` form still exists in some files but is not the convention for new code.
- `TError` is a `record` or `sealed class` deriving from `Exception` (or implementing `IError`) and is defined in the Application layer next to the handler that throws it.
- Don't catch exceptions in the resolver — let them propagate. The mutation convention pipeline maps them to the declared union.

## Authorization

Two layers:

1. **GraphQL-level (the coarse gate)** — `[Authorize]` on mutations, `[Authorize(Policy = "<policy-name>")]` on sensitive fields. This rejects requests before any handler runs.
2. **Application-level (the real check)** — every query handler and command handler calls `IAuthorizationService` against the `ClaimsPrincipal` to enforce tenant / role / resource permissions. **This is where the real authorization happens.** Read [`query`](../query/SKILL.md) and [`command`](../command/SKILL.md) for the patterns.

GraphQL-level authorization is a quick-fail. Application-level authorization is the source of truth. Both run; one is not a substitute for the other.

## Wrong vs right

**Wrong** — business logic, hand-rolled input, DbContext in the resolver:

```csharp
[MutationType]
public class BookMutations
{
    public async Task<CreateBookPayload> CreateBook(
        CreateBookInput input,         // hand-rolled input — breaks mutation conventions
        BookDbContext db,              // DbContext in a GraphQL resolver
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title))   // business validation in GraphQL
            throw new Exception("title required");

        var book = new Book { Title = input.Title };
        db.Books.Add(book);                            // persistence in the resolver
        await db.SaveChangesAsync(ct);

        return new CreateBookPayload(book);            // hand-rolled payload
    }
}
```

**Right** — thin wrapper, parameters wrapped by the convention, command does the work:

```csharp
using Mocha.Mediator;

[MutationType]
public class BookMutations
{
    [Authorize]
    [Error<DuplicateTitleError>]
    [Error<UnauthorizedOperation>]
    public async ValueTask<Book> CreateBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid authorId,
        string title,
        CancellationToken ct)
        => await sender.SendAsync(new CreateBookCommand(user, authorId, title), ct);
}
```

The convention turns `(authorId, title)` into `CreateBookInput { authorId, title }` and wraps the `Book` return in `CreateBookPayload { book, errors }`.

## Gotchas

- **No `Input` types in `<Entity>/Inputs/` unless they're shared across mutations or use `@oneOf`.** Mutation conventions auto-generate per-mutation inputs. Hand-roll only for cross-cutting concerns.
- **Don't return `ValueTask<TPayload>`.** Return `ValueTask<TEntity>` and let the convention wrap. Returning a hand-rolled payload disables the convention's error-union mapping.
- **`[NodeResolver]` is mandatory for every entity in the graph.** Without it, Relay refetches fail and global IDs aren't queryable via `node(id:)`. Missing node resolvers are caught at startup with an obvious error.
- **Don't bypass DataLoaders.** Resolvers that load a related entity dispatch the matching query through the mediator (the handler uses a DataLoader internally) — never `context.<Set>.FirstOrDefaultAsync(...)`. See [`dataloader`](../dataloader/SKILL.md).
- **Don't mix `[Error<T>]` and `[Error(typeof(T))]` in the same file.** Pick the generic form for the whole file. New code is always generic.
- **`descriptor.Ignore(x => ...)` for raw FKs and unloaded navigations.** If `Book.AuthorId` is exposed as an `Author` via a resolver, ignore `AuthorId`. If `Book.Reviews` is paged via a `[UsePaging]` resolver, ignore the raw `Reviews` collection.

## References

- [TYPES](references/TYPES.md) — `[ObjectType<T>]`, `Configure`, resolver methods, `[BindMember]`, `[UsePaging]`, `Connection<T>`, `[Parent]`, `IResolverContext`.
- [QUERIES](references/QUERIES.md) — `[QueryType]`, dispatching queries via `ISender.QueryAsync`, ID parameters, service injection.
- [MUTATIONS](references/MUTATIONS.md) — `[MutationType]`, `[Error<T>]`, `[Authorize]`, dispatching commands.
- [NODE-RESOLVER](references/NODE-RESOLVER.md) — global object identification, simple-delegate vs auth-handler forms.
- [MUTATION-CONVENTIONS](references/MUTATION-CONVENTIONS.md) — why auto-wrapping inputs/payloads is mandatory.
- Related skills: [`dataloader`](../dataloader/SKILL.md), [`query`](../query/SKILL.md), [`command`](../command/SKILL.md), [`graphql-schema-design`](../graphql-schema-design/SKILL.md).
