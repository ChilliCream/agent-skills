---
name: query
description: Author application-layer queries in the Mocha mediator CQRS style — a Mocha mediator IQuery record + IQueryHandler that loads through DataLoaders, authorizes through IAuthorizationService, and returns null on failure. Fire when the user asks to "add a query", "add a get-by-id", "load <Entity> by <Key>", asks for an "application query" or "read operation", mentions `IQuery`, `IQueryHandler`, `QueryAsync`, `HandleAsync`, when editing any file under `*/Application/<Entity>/Queries/*`, when a `[QueryType]` GraphQL resolver needs a backing query handler, or when reviewing read-paths that touch `IAuthorizationService` + DataLoaders. Use this skill — do not improvise — because Mocha's mediator routes queries through `ISender.QueryAsync` (a distinct method from `SendAsync`), and the auth/null-return rules are easy to get wrong.
---

# query

Write one application-layer read operation as a Mocha mediator query: an `IQuery<TResponse>` record and an `IQueryHandler<TQuery, TResponse>` whose `HandleAsync` method:

1. Rejects unauthenticated users.
2. Loads the entity through a DataLoader (or a generated `[DataLoaderGroup]` batching context).
3. Calls `IAuthorizationService.AuthorizeAsync` against a project-defined policy string (for example `"Books.Read"`).
4. Returns `null` — never throws — when auth, lookup, or permission fails.

The handler is discovered by the Mocha source generator and dispatched via `ISender.QueryAsync`. Both commands and queries flow through the same mediator; queries are first-class messages, not "plain DI classes". The narrow `QueryAsync` method makes the intent explicit (no state change, side-effect-free read).

## Why Mocha mediator, not MediatR

Mocha keeps in-process CQRS (mediator) and cross-service messaging (bus) under one set of primitives, so middleware, instrumentation, and source-generator tooling stay consistent across both. Queries get the same logging, instrumentation, and EF transaction-skipping middleware as commands without writing a different dispatch surface.

## Mocha mediator vs. Mocha message bus

Mocha bundles two dispatch mechanisms. The **mediator** (`IMediator` / `ISender` / `IPublisher` in `Mocha.Mediator`) is for in-process CQRS within a single service. The **message bus** (`IMessageBus` in `Mocha`) is for cross-service / cross-process messaging. They share the same Roslyn source-generator infrastructure and middleware model but are separate primitives. Queries always go through the **mediator** — never the message bus.

## File location and naming

```
src/MyApp.Application/Books/Queries/Get<Entity>By<Criterion>Query.cs
src/MyApp.Application/Books/Queries/Get<Entity>By<Criterion>QueryHandler.cs   # or in the same file
```

- Query record and handler can live in the same file or split — pick the convention already used in the entity's folder.
- Singular: `GetBookByIdQuery`, not `GetBooksByIdQuery`.
- For lists, name `Get<Plural>By<Parent>Query` returning `IReadOnlyList<T>?`. For pagination, name `Page<Plural>By<Parent>Query` returning `Page<T>?`.
- The `Query` suffix is part of the type name — the Roslyn analyzer pairs `<X>Query` with `<X>QueryHandler` by interface satisfaction, but the suffix keeps the codebase searchable.

## Canonical example

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;

namespace MyApp.Application.Books.Queries;

public sealed record GetBookByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Book?>;

public sealed class GetBookByIdQueryHandler(
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBookByIdQuery, Book?>
{
    public async ValueTask<Book?> HandleAsync(
        GetBookByIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, id) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var book = await books.BookById.LoadAsync(id, cancellationToken);
        if (book is null)
        {
            return null;
        }

        var auth = await authorization.AuthorizeAsync(user, book, "Books.Read");
        if (!auth.Succeeded)
        {
            return null;
        }

        return book;
    }
}
```

Notice:

- **`IQuery<Book?>`** — the response type is `Book?`. Nullability is part of the contract: `null` means "you cannot see this".
- **`IQueryHandler<GetBookByIdQuery, Book?>`** — generic on the query type and the response type, both visible at compile time.
- **`HandleAsync`** returning **`ValueTask<Book?>`** — never `Task<Book?>`. Mocha's pipeline is `ValueTask`-based end-to-end.
- **Primary constructors** on the record and the handler — no field assignments, no `: base()` ceremony.
- **`sealed`** on both — the source generator routes on the runtime type and handlers are never subclassed.
- **`ClaimsPrincipal` is part of the query record**, not a separate parameter on `HandleAsync`. This keeps the dispatch surface uniform: every query is a single object.
- Injects `IBookBatchingContext` (generated from `[DataLoaderGroup("BookBatchingContext")]`) — *not* a bare `IBookByIdDataLoader`. Batching contexts expose every DataLoader in the group, which avoids constructor churn when more loaders are added.
- `CancellationToken` is the last parameter on `HandleAsync` and is **required** — no default value (matches the Mocha `IQueryHandler<,>` interface).

The policy string `"Books.Read"` is project-defined. Your project wires up its own `IAuthorizationHandler` implementations (or `AuthorizationOptions.AddPolicy(...)` registrations) against that name.

## Dispatching a query

Inject `ISender` (or `IMediator` if you also publish) and call `QueryAsync`:

```csharp
var book = await sender.QueryAsync(new GetBookByIdQuery(user, id), cancellationToken);
```

`ISender.QueryAsync` is a **distinct method** from `SendAsync`:

| Dispatch method | Accepts | Don't confuse with |
|---|---|---|
| `sender.SendAsync(cmd, ct)` | `ICommand` / `ICommand<TResponse>` | calling with a query won't compile |
| `sender.QueryAsync(qry, ct)` | `IQuery<TResponse>` | calling with a command won't compile |
| `publisher.PublishAsync(evt, ct)` | `INotification` | unrelated — fan-out, no response |

Picking the wrong one is a compile error, not a runtime surprise.

## The GraphQL wrapper (thin)

A `[QueryType]` class dispatches via `ISender.QueryAsync`. It contains zero business logic.

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
        return sender.QueryAsync(new GetBookByIdQuery(user, id), cancellationToken).AsTask();
    }
}
```

Notice:

- `ISender` is parameter-injected (per-request scope), not constructor-injected.
- `[ID(nameof(Book))]` wraps the GUID as a Relay node id at the GraphQL boundary. The query record always sees a plain `Guid`.
- No try/catch, no transformation. If the handler returns null, GraphQL surfaces the field as null cleanly.
- `.AsTask()` adapts the handler's `ValueTask<Book?>` to the `Task<Book?>` return that the GraphQL middleware expects in many resolver signatures. When the surrounding code accepts `ValueTask<T>` directly, drop the `.AsTask()` call.

For richer GraphQL wrapper patterns (pagination args, projection, node resolvers) see [references/GRAPHQL-WRAPPER.md](references/GRAPHQL-WRAPPER.md).

## Authorization flow — strict order

Follow this order. Reordering changes the failure mode and leaks information.

1. **Authentication check first.** `if (user.Identity is not { IsAuthenticated: true }) return null;`
   - Why first: avoids loading anything for anonymous traffic. No DB hit, no DataLoader batch participation.
2. **Load the entity via DataLoader.** `var entity = await loader.LoadAsync(id, ct);`
   - Why DataLoader (and not `IAppDbContext` directly): DataLoaders batch and cache per request, and they share the same promise cache with GraphQL resolvers downstream. A direct `DbContext` query bypasses both.
3. **Existence check.** `if (entity is null) return null;`
   - Returning null for "not found" is intentional: it makes the caller indistinguishable from "exists but no permission". That's the point — see the leak note below.
4. **Permission check.** `var auth = await authorization.AuthorizeAsync(user, entity, "<Resource>.<Action>"); if (!auth.Succeeded) return null;`
   - Pass the loaded entity, *not* the id. The authorization service runs your project-defined `IAuthorizationHandler<TRequirement, TResource>` against the actual object so the handler can inspect ownership, tenancy, or any other resource-bound rule.
5. **Return the entity.** No projection, no cloning. The caller (or GraphQL resolver) handles shaping.

### Why return null instead of throwing

Throwing an `UnauthorizedAccessException` or a `NotFound` error from a query leaks the existence of the entity through GraphQL's error path:

- The `errors` array tells an attacker the id *exists* (because they got `Unauthorized`) versus *does not* (`NotFound`). Returning null collapses both paths to "you cannot see this".
- Throwing also forces every consuming resolver to handle the error or pollute the response with a partial-error.
- GraphQL semantics already model "field has no value" — use them.

Commands throw because callers are *acting* on a known entity; queries return null because callers are *asking* whether they can see one.

> **Mocha note:** a query handler that returns a non-null successful response cannot return `null` for the response type unless the response type is itself nullable. `IQuery<Book?>` is fine; `IQuery<Book>` is not — Mocha rejects `null` responses at runtime. Declare the nullable.

### Why never write a manual membership check

Reaching into `context.Set<Membership>()` to verify access bypasses the entire authorization stack: requirement composition, role inheritance, scoped policies, the resource-based handlers you've registered. Always go through `IAuthorizationService.AuthorizeAsync` so every policy your project defines gets a chance to evaluate.

## Wrong vs right

```csharp
// WRONG — old plain-class pattern with ExecuteAsync. Pre-Mocha shape.
public sealed class GetBookById(
    IBookBatchingContext books,
    IAuthorizationService authorization)
{
    public async Task<Book?> ExecuteAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        // ...
    }
}
```

```csharp
// RIGHT — Mocha IQuery + IQueryHandler with HandleAsync
public sealed record GetBookByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Book?>;

public sealed class GetBookByIdQueryHandler(
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBookByIdQuery, Book?>
{
    public async ValueTask<Book?> HandleAsync(GetBookByIdQuery query, CancellationToken ct)
    {
        // ...
    }
}
```

```csharp
// WRONG — dispatching a query via SendAsync (compile error)
var book = await sender.SendAsync(new GetBookByIdQuery(user, id), ct);
```

```csharp
// RIGHT — QueryAsync
var book = await sender.QueryAsync(new GetBookByIdQuery(user, id), ct);
```

```csharp
// WRONG: throws on not-found / no-permission — leaks existence through GraphQL errors
public async ValueTask<Book> HandleAsync(GetBookByIdQuery query, CancellationToken ct)
{
    var book = await books.BookById.LoadAsync(query.Id, ct)
        ?? throw new InvalidOperationException("Book not found");

    var auth = await authorization.AuthorizeAsync(query.User, book, "Books.Read");
    if (!auth.Succeeded)
    {
        throw new UnauthorizedAccessException();
    }

    return book;
}
```

```csharp
// WRONG: hand-rolls a membership check — bypasses your authorization handlers entirely
public async ValueTask<Book?> HandleAsync(GetBookByIdQuery query, CancellationToken ct)
{
    var book = await context.Books
        .Include(x => x.Author)
        .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

    if (book is null) return null;

    var userId = query.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var owns = await context.Set<AuthorMembership>()
        .AnyAsync(x => x.AuthorId == book.AuthorId && x.UserId == userId, ct);

    return owns ? book : null;
}
```

```csharp
// RIGHT — see canonical example above. Batching context, return null, permission via service.
public sealed class GetBookByIdQueryHandler(
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBookByIdQuery, Book?> { ... }
```

```csharp
// WRONG: injects individual DataLoader instead of the batching context
public sealed class GetBookByIdQueryHandler(
    IBookByIdDataLoader bookById,                   // <-- one-off injection
    IBookEditionByIdDataLoader bookEditionById,     // <-- adds friction every time the query grows
    IAuthorizationService authorization)
    : IQueryHandler<GetBookByIdQuery, Book?> { ... }
```

## DI registration

Query handlers are picked up by the Mocha Roslyn source generator. Register the module once in your composition root:

```csharp
services
    .AddMediator()
    .AddApplication();   // source-generated from the assembly name
```

The generated `Add{Module}()` extension method is named after the last segment of the assembly name (`MyApp.Application` → `AddApplication()`). To override, add `[assembly: MediatorModule("Application")]` to any file in the project.

Default handler lifetime is `Scoped`, which matches the GraphQL request lifetime and the DataLoader scope. Change with `.ConfigureOptions(o => o.ServiceLifetime = ServiceLifetime.Transient)` if needed — but the default is what you want for queries that share the promise cache with sibling resolvers.

Do not register individual handlers manually. `AddHandler<T>()` exists as an escape hatch for plugin assemblies and integration tests, but the canonical path is the generated module.

## When the input isn't an id

The pattern generalizes:

- `GetBookByTitleQuery(ClaimsPrincipal User, string Title) : IQuery<Book?>` — looks up via a `BookByTitle` DataLoader.
- `GetBooksByAuthorIdQuery(ClaimsPrincipal User, Guid AuthorId) : IQuery<IReadOnlyList<Book>?>` — returns `IReadOnlyList<Book>?`, still null on auth failure, still single permission check on the parent (author).
- `PageBooksByAuthorIdQuery(ClaimsPrincipal User, Guid AuthorId, PagingArguments Paging) : IQuery<Page<Book>?>` — authorizes the parent (`Author`) with `"Books.Read"`, then returns the paged DataLoader result.

Whatever the shape: authentication check → load via DataLoader → permission check on the entity (or the relevant *parent* when the result is a collection) → return result or null.

For longer handler examples (paged lists, post-mutation re-read) see [references/HANDLERS.md](references/HANDLERS.md).

## Pre-commit checklist

- [ ] File path: `<App>.Application/Books/Queries/Get<Entity>By<Criterion>Query.cs` (+ optional `...QueryHandler.cs`).
- [ ] Query is a `sealed record` implementing `IQuery<TResponse>`. Response type is **nullable** when null is the failure path.
- [ ] Handler is a `sealed class` with a primary constructor implementing `IQueryHandler<TQuery, TResponse>`.
- [ ] Injects the `I<Entity>BatchingContext` (if a `[DataLoaderGroup]` exists for the entity) or the specific `I<Entity>ByIdDataLoader` interface, plus `IAuthorizationService`.
- [ ] `HandleAsync(<Query> query, CancellationToken cancellationToken)` returning `ValueTask<TResponse>` — not `Task`, not `Handle`.
- [ ] Order in `HandleAsync`: `IsAuthenticated` → `LoadAsync` → null check → `AuthorizeAsync` → return.
- [ ] Returns `null` (not throws) on every failure path.
- [ ] Uses `Mocha.Mediator` (`IQuery`, `IQueryHandler`) — never `MediatR.IRequest`.
- [ ] `cancellationToken` has no default value.
- [ ] Permission policy is a project-defined string (`"Books.Read"`, `"Books.Write"`, etc.) — never a hand-rolled comparison.
- [ ] Mediator registered with `services.AddMediator().Add{Module}()` in the composition root (Mocha source generator wires the handler in).
- [ ] Thin `[QueryType]` wrapper dispatches via `ISender.QueryAsync(...)`.

## Related skills

- `command` — the write-side counterpart. Same mediator, different dispatch verb (`SendAsync`) and failure model (throw, don't null).
- `graphql-backend` — the `[QueryType]` host pattern and resolver conventions.
- `dataloader` — how to author the `I<Entity>BatchingContext` that handlers inject.

## References

- [GRAPHQL-WRAPPER.md](references/GRAPHQL-WRAPPER.md) — pagination args, projection, `[ID]` mapping, when to add `[UsePaging]` vs. when the handler already pages, node resolvers.
- [HANDLERS.md](references/HANDLERS.md) — list and pagination handler shapes, lookup-by-name, and post-mutation re-read patterns.
