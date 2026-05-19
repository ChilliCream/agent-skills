# GraphQL mutation wrapper

Detailed patterns for the HotChocolate side of a command — the `[MutationType]` class that calls `ISender.SendAsync`. The main skill body covers the basics; this file covers the traps.

## File location and class shape

One mutations file per entity, e.g. `src/MyApp.GraphQL/Books/BookMutations.cs`.

```csharp
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types.Relay;
using Mocha.Mediator;

namespace MyApp.GraphQL.Books;

[MutationType]
public class BookMutations
{
    [Authorize]
    [Error<AuthorNotFoundException>]
    [Error<UnauthorizedAccessException>]
    public async Task<Book> CreateBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Author))] Guid authorId,
        string title,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookCommand(user, authorId, title);
        return await sender.SendAsync(command, cancellationToken);
    }
}
```

Key rules:

- One method per command. Method name ends in `Async`.
- The class is annotated with `[MutationType]` (source generator) — do not `extends` anything.
- The method must accept `ISender` and `CancellationToken` as parameters. Inject `ClaimsPrincipal` directly so you can pass it to the command record.
- Parameters that are entity IDs use `[ID]`. Plain `Guid` will not decode Relay global IDs.

### Which mediator interface to inject

| Interface | When to inject |
|---|---|
| `ISender` | Mutation only dispatches commands (or queries). This is the default for `[MutationType]` methods. |
| `IPublisher` | Resolver only publishes notifications. |
| `IMediator` | Resolver does both. `IMediator : ISender, IPublisher`, so it's a strict superset. |

Default to `ISender`. Pick the narrowest surface — it makes the call site's intent obvious and makes mocking in tests easier.

## Mutation Conventions: do not hand-roll input types

HotChocolate's Mutation Conventions wrap the method parameters into a generated `<MutationName>Input` type, and the return value into a `<MutationName>Payload`. The schema gets `createBook(input: CreateBookInput!): CreateBookPayload!` for free.

Right — direct method parameters:

```csharp
public async Task<Book> CreateBookAsync(
    ClaimsPrincipal user,
    ISender sender,
    [ID(nameof(Author))] Guid authorId,
    string title,
    CancellationToken cancellationToken)
{
    var command = new CreateBookCommand(user, authorId, title);
    return await sender.SendAsync(command, cancellationToken);
}
```

Wrong — hand-rolled input record:

```csharp
public sealed record CreateBookInput([ID<Author>] Guid AuthorId, string Title);

public async Task<Book> CreateBookAsync(
    CreateBookInput input,
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken cancellationToken)
{
    var command = new CreateBookCommand(user, input.AuthorId, input.Title);
    return await sender.SendAsync(command, cancellationToken);
}
```

What goes wrong: the conventions still wrap, so the schema gets `CreateBookInput` → `CreateBookInputInput`. You end up with `input.input.authorId` on the client side. Delete the input record and pass parameters directly.

The same applies to payloads — do not hand-roll a `CreateBookPayload`. The conventions emit one from the return type, including the union of `Error<T>` types.

## Use `[Error<T>]`, not `[Error(typeof(T))]`

The generic form `[Error<TError>]` is the one HotChocolate's source generator and schema builder pick up cleanly. The non-generic `[Error(typeof(T))]` carries the type only as `System.Type` metadata and loses the compile-time link, which causes:

- Schema generation can miss the error variant in the payload union.
- IDE rename refactors stop working across the boundary.
- The analyzer can't enforce that the command actually throws the declared error.

Right:

```csharp
[Error<AuthorNotFoundException>]
[Error<UnauthorizedAccessException>]
[Error<BookDeletionFailedException>]
public async Task<Book> DeleteBookByIdAsync(...) { ... }
```

Wrong:

```csharp
[Error(typeof(AuthorNotFoundException))]
[Error(typeof(UnauthorizedAccessException))]
public async Task<Book> DeleteBookByIdAsync(...) { ... }
```

Declare every error the handler can throw — auth, not-found, validation. The list is part of the GraphQL schema; missing entries mean clients can't type-narrow on them.

## `[ID]` variants

Two equivalent ways to mark a Relay global ID parameter:

```csharp
[ID(nameof(Author))] Guid authorId
[ID<Author>] Guid authorId
```

Both decode the incoming base64 Relay ID before the resolver runs. Pick whichever is already used in the file. The bare `Guid authorId` without `[ID]` will accept raw UUIDs but break Relay clients that always send global IDs.

For `NodeResolver` (the Query side, not mutations), use `[ID] string id` — the global ID arrives as a base64 string and you parse it yourself.

## Post-mutation re-read pattern

Some mutations have side effects beyond returning the mutated entity (e.g., audit logs, cache invalidation across services) and need a fresh read to ensure the response reflects the latest state. The pattern: send the command, then dispatch the matching `Get<Entity>ByIdQuery` via `QueryAsync`.

```csharp
[Authorize]
[Error<BookNotFoundException>]
[Error<UnauthorizedAccessException>]
public async Task<Book> UpdateBookTitleAsync(
    ClaimsPrincipal user,
    ISender sender,
    [ID(nameof(Book))] Guid bookId,
    string newTitle,
    CancellationToken cancellationToken)
{
    var command = new UpdateBookTitleCommand(user, bookId, newTitle);
    await sender.SendAsync(command, cancellationToken);

    return (await sender.QueryAsync(new GetBookByIdQuery(user, bookId), cancellationToken))!;
}
```

Two reasons to do this rather than return the handler's result:

1. The Query handler enforces the **read** permission, so the response respects the same auth surface as a direct read.
2. The Query goes through DataLoaders, so the post-mutation read shares the cache with sibling resolvers in the same request.

Only adopt this pattern when the value matters. For most creates/deletes, return the handler's result directly.

> Note: `sender.SendAsync` for the command and `sender.QueryAsync` for the query are **different** methods on `ISender`. They're not interchangeable — calling `SendAsync` with an `IQuery<T>` is a compile error (and vice versa).

## `[Authorize]` at the method level

Add `[Authorize]` to every mutation method. This trips HotChocolate's global authorization handler **before** the resolver runs — anonymous requests get rejected before they reach the handler's auth gate. The command's `ClaimsPrincipal` check remains the second line of defense (handlers are called from non-GraphQL paths in tests).

## Naming

- Mutation method: `<Verb><Entity>Async` (`CreateBookAsync`, `DeleteBookByIdAsync`, `UpdateBookTitleAsync`).
- The schema emits `createBook`, `deleteBookById`, `updateBookTitle` — HotChocolate strips the `Async` suffix and lowerCamels the rest.
- Stay consistent with the command record name: `CreateBookCommand` → `CreateBookAsync`.

## Full reference example

A mutations class with multiple commands:

```csharp
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types.Relay;
using Mocha.Mediator;

[MutationType]
public class BookMutations
{
    [Authorize]
    [Error<BookNotFoundException>]
    [Error<UnauthorizedAccessException>]
    public async Task<Book> UpdateBookTitleAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Book))] Guid bookId,
        string newTitle,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBookTitleCommand(user, bookId, newTitle);
        await sender.SendAsync(command, cancellationToken);
        return (await sender.QueryAsync(new GetBookByIdQuery(user, bookId), cancellationToken))!;
    }

    [Authorize]
    [Error<BookNotFoundException>]
    [Error<UnauthorizedAccessException>]
    [Error<BookDeletionFailedException>]
    public async Task<Book> DeleteBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Book))] Guid bookId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBookByIdCommand(user, bookId);
        return await sender.SendAsync(command, cancellationToken);
    }
}
```

## Common mistakes

- **Injecting `IMediator` when only commands are dispatched.** Prefer `ISender`. Reach for `IMediator` only when the same method also publishes notifications.
- **Calling `mediator.Send(command, ct)`.** That's MediatR's API. Mocha is `sender.SendAsync(command, ct)` — note the `Async` suffix and the `ValueTask` return.
- **Calling `SendAsync` on a query.** Queries go through `QueryAsync`. `SendAsync(IQuery<T>)` doesn't compile against `ISender`.
- **Forgetting `[Authorize]`.** Lets unauthenticated requests hit the handler's gate when the network gate should reject them. Add it on every method.
- **Mixing `[Error<T>]` and `[Error(typeof(T))]` in the same file.** Pick the generic form. The non-generic form is a holdover.
- **Adding a hand-rolled `Input` record.** Mutation Conventions already wrap the parameters. Hand-rolling doubles the wrap.
- **Returning the wrong type.** Return the entity (`Task<Book>`), not the command result wrapped in some response DTO. Conventions build the payload.
- **Missing `[ID]` on Guid parameters.** Relay clients send base64-encoded global IDs; bare `Guid` fails to decode.
- **One mutation class per command.** Wrong — one mutations class per entity, all the entity's commands as methods on it.
