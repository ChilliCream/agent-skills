# MUTATIONS — `[MutationType]`

Mutation methods are thin adapters. Take GraphQL arguments, build a Mocha `ICommand` / `ICommand<T>`, dispatch via `ISender.SendAsync`, return the entity. The command handler — in the Application layer — does the actual work. See [`command`](../../command/SKILL.md).

## Anatomy

```csharp
using Mocha.Mediator;

[MutationType]
public class BookMutations
{
    [Authorize]
    [Error<BookNotFoundError>]
    [Error<UnauthorizedOperation>]
    [Error<BookDeletionFailedError>]
    public async ValueTask<Book> DeleteBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid bookId,
        CancellationToken ct)
        => await sender.SendAsync(new DeleteBookByIdCommand(user, bookId), ct);
}
```

Three responsibilities:

1. Translate GraphQL arguments to a command (no validation, no business logic).
2. Dispatch via `ISender.SendAsync`.
3. Return the resulting entity.

Mutation conventions wrap the arguments into a generated `DeleteBookByIdInput { bookId }` and wrap the return into a generated `DeleteBookByIdPayload { book, errors }`. See [MUTATION-CONVENTIONS](MUTATION-CONVENTIONS.md).

## Required parameters

Every mutation injects:

```csharp
public async ValueTask<T> MutationAsync(
    /* services */
    ClaimsPrincipal user,
    ISender sender,

    /* arguments — primitives, IDs, complex inputs */
    [ID<Entity>] Guid entityId,
    string someValue,

    CancellationToken ct)
```

- `ClaimsPrincipal user` — the authenticated principal. **Always pass it into the command** so the handler can authorize against it.
- `ISender` — for dispatching commands and queries. Prefer this over the broader `IMediator`. Inject `IMediator` only when the resolver also publishes notifications.
- `CancellationToken` — required, no default.

## Mocha dispatch surface

| Use | Call |
|---|---|
| Send a void command | `await sender.SendAsync(command, ct);` |
| Send a command with response | `var result = await sender.SendAsync(command, ct);` |
| Send a query (read) | `var result = await sender.QueryAsync(query, ct);` |
| Publish a notification | `await publisher.PublishAsync(notification, ct);` (inject `IPublisher` or `IMediator`) |

`SendAsync` and `QueryAsync` are **distinct methods** on `ISender`. Calling `SendAsync` with an `IQuery<T>` is a compile error and vice versa. All return `ValueTask` / `ValueTask<T>` — `await` them once; use `.AsTask()` to adapt to `Task<T>` only when the surrounding GraphQL middleware specifically expects it.

## Return types

Return the **entity type**, not a payload:

```csharp
public async ValueTask<Book> CreateBookAsync(...)
    => await sender.SendAsync(command, ct);   // command handler returns Book
```

The mutation convention wraps `Book` into `CreateBookPayload { book, errors }`. If you return `ValueTask<CreateBookPayload>` yourself, you defeat the convention and the error-union no longer maps.

Special cases:

- Returning a different type than the command handler: useful when the mutation refetches a richer view. Dispatch the command, then dispatch the matching query:
  ```csharp
  await sender.SendAsync(command, ct);
  return (await sender.QueryAsync(new GetBookByIdQuery(user, id), ct))!;
  ```
- Returning a non-entity result: `ValueTask<Guid>` for "request id" patterns, `ValueTask<bool>` for void-ish operations. The convention still wraps the value into a payload field.

## Error declaration — `[Error<T>]`

Every domain error the mutation may surface is declared with `[Error<TError>]`. HotChocolate generates a discriminated union of error types per mutation.

```csharp
[Authorize]
[Error<BookNotFoundError>]
[Error<UnauthorizedOperation>]
[Error<BookDeletionFailedError>]
public async ValueTask<Book> DeleteBookByIdAsync(...) { ... }
```

Rules:

- Always use the generic form `[Error<T>]` for new code. The legacy `[Error(typeof(T))]` form still exists in older files but is not the convention. Don't mix forms in the same file.
- `TError` is defined in the Application layer next to the command handler that throws it. It is a `sealed class` deriving from `Exception` or a `record : IError`.
- The error type should expose enough data for clients to react:

  ```csharp
  public sealed class BookNotFoundError(Guid bookId) : Exception("Book was not found")
  {
      [ID<Book>]
      public Guid BookId { get; } = bookId;
  }
  ```

  Re-encode IDs with `[ID<Entity>]` so clients receive Relay global IDs.

Errors thrown by the command handler are caught by the mutation pipeline and mapped to the GraphQL error union. The resolver does **not** catch them.

## Authorization

Two-layer model:

```csharp
[Authorize]                                                 // GraphQL gate
[Error<UnauthorizedOperation>]
public async ValueTask<Book> UpdateBookTitleAsync(
    ClaimsPrincipal user,                                   // for the handler
    ISender sender,
    [ID<Book>] Guid bookId,
    string title,
    CancellationToken ct)
    => await sender.SendAsync(new UpdateBookTitleCommand(user, bookId, title), ct);
    // -> handler calls IAuthorizationService.HasPermissionAsync(user, ...)
    // -> throws UnauthorizedOperation if denied
    // -> declared as [Error<UnauthorizedOperation>] above so the union maps it
```

- `[Authorize]` (no policy) — requires an authenticated user. Default for most mutations.
- `[Authorize(Policy = "BooksWrite")]` — when the field needs a coarse policy. Real per-resource auth still happens in the handler.
- No `[Authorize]` at all — rare; only for explicitly anonymous endpoints. The handler must still validate.

## `[UseMutationConvention(...)]` overrides

The default conventions are fine for ~90% of mutations. Override only when you need a non-default payload shape:

```csharp
[UseMutationConvention(PayloadFieldName = "id")]
[ID("BookPublishRequest")]
public async ValueTask<Guid> PublishBookAsync(...) { ... }
```

Means: the payload field for the returned `Guid` is named `id` (not `guid`), and that field is exposed as a Relay ID of type `BookPublishRequest`.

Override sparingly. Whenever possible, return the entity and let conventions name the payload field after it.

## Inputs — almost never hand-rolled

The mutation convention auto-generates `<MutationName>Input` from the method parameters. Hand-rolling an input type is wrong in almost every case.

Hand-roll an input type only when:

- The input is **shared** across multiple mutations.
- The input uses `@oneOf` (exactly one of N fields must be set).

For the shared case, pass the input as a parameter — the convention still wraps it under the outer `<MutationName>Input`. See [MUTATION-CONVENTIONS](MUTATION-CONVENTIONS.md) for the full reasoning.

## File uploads

`IFile` is HotChocolate's upload type. Stream from `OpenReadStream()` into the command:

```csharp
public async ValueTask<BookFile> UploadBookFileAsync(
    [ID<Book>] Guid bookId,
    IFile file,
    string description,
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken ct)
{
    await using var stream = file.OpenReadStream();
    var command = new UploadBookFileCommand(user, bookId, stream, description);
    return await sender.SendAsync(command, ct);
}
```

`await using` is mandatory — the stream is request-scoped and `IDisposable`.

## Wrong vs right

**Wrong — MediatR API (`IMediator.Send`, `Task<T>` return on the handler):**

```csharp
public async Task<Book> DeleteBookByIdAsync(
    ClaimsPrincipal user,
    IMediator mediator,                                  // MediatR shape
    [ID<Book>] Guid bookId,
    CancellationToken ct)
{
    var command = new DeleteBookByIdCommand(user, bookId);
    return await mediator.Send(command, ct);             // Send, not SendAsync
}
```

**Right — Mocha mediator (`ISender.SendAsync`, `ValueTask<T>` from the handler):**

```csharp
public async ValueTask<Book> DeleteBookByIdAsync(
    ClaimsPrincipal user,
    ISender sender,
    [ID<Book>] Guid bookId,
    CancellationToken ct)
    => await sender.SendAsync(new DeleteBookByIdCommand(user, bookId), ct);
```

**Wrong — hand-rolled input + payload:**

```csharp
public record CreateBookInput([property: ID<Author>] Guid AuthorId, string Title);
public record CreateBookPayload(Book Book);

[MutationType]
public class BookMutations
{
    public async Task<CreateBookPayload> CreateBook(   // hand-rolled payload return
        CreateBookInput input,                          // hand-rolled input
        ISender sender,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var book = await sender.SendAsync(
            new CreateBookCommand(user, input.AuthorId, input.Title), ct);
        return new CreateBookPayload(book);
    }
}
```

Why this is wrong: hand-rolling defeats mutation conventions. The generated GraphQL field becomes `createBook(input: CreateBookInput!): CreateBookPayload!`, which looks fine, but you've now bypassed the error-union mapping. `[Error<T>]` no longer wraps thrown exceptions into the payload's `errors` field, because the convention's middleware doesn't recognize the hand-rolled payload type.

**Right — flat parameters, entity return:**

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

The convention generates the same schema (`createBook(input: CreateBookInput!): CreateBookPayload!`) plus the error union (`CreateBookError = DuplicateTitleError | UnauthorizedOperation`), and the middleware wires up the error mapping.

**Wrong — business logic in the resolver:**

```csharp
public async Task<Book> DeleteBookByIdAsync(...)
{
    var book = await db.Books.FindAsync(bookId);            // DbContext
    if (book == null) throw new BookNotFoundError(bookId);  // logic
    if (book.OwnerId != currentUserId)                      // auth check
        throw new UnauthorizedOperation(...);
    db.Books.Remove(book);                                  // persistence
    await db.SaveChangesAsync(ct);
    return book;
}
```

**Right — one-line delegate to the command:**

```csharp
public async ValueTask<Book> DeleteBookByIdAsync(
    ClaimsPrincipal user,
    ISender sender,
    [ID<Book>] Guid bookId,
    CancellationToken ct)
    => await sender.SendAsync(new DeleteBookByIdCommand(user, bookId), ct);
```

The command handler does the lookup, the auth check, the deletion, and throws `BookNotFoundError` / `UnauthorizedOperation` / `BookDeletionFailedError` as appropriate.

## Gotchas

- **Injecting `IMediator` everywhere** — prefer `ISender` for command-only resolvers. Use `IMediator` only when the same method also publishes a notification (or when test fakes need both surfaces).
- **Calling `Send` instead of `SendAsync`** — MediatR muscle memory. Mocha is `SendAsync` (and the result is `ValueTask<T>`, not `Task<T>`).
- **Calling `SendAsync` on a query** — queries go through `QueryAsync`. Compile error if you cross them.
- **Forgetting `ClaimsPrincipal` in the command** — the handler has no user context. Authorization checks silently default to "deny all" or, worse, "allow all" depending on the handler. Always: `new <T>Command(user, ...)`.
- **Catching exceptions in the resolver** — breaks `[Error<T>]` mapping. Let exceptions propagate. The convention catches them and maps them to the union.
- **Mixing `[Error<T>]` and `[Error(typeof(T))]`** — works but is inconsistent. Pick the generic form for the whole file.
- **`[Authorize]` policy mismatch with the handler's check** — leads to "allowed at GraphQL but denied in the handler" or vice versa. The handler's check is the source of truth; `[Authorize]` is the coarse gate. If they disagree, fix one to match the other.
- **Returning `ValueTask<TPayload>`** — disables the mutation convention's payload wrapping. Return the entity.
- **Skipping `[Error<T>]`** — exceptions thrown by the handler still propagate, but they surface as generic GraphQL errors rather than typed members of the mutation's error union. Clients lose the ability to discriminate. Declare every domain error.
