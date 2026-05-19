---
name: command
description: Author Mocha mediator command handlers in the application layer, wire them through HotChocolate mutations using mutation conventions, and write the matching command tests. Fire whenever the user says "add a command", "Mocha command", "ICommand", "command handler", "mutation handler", "write a mutation", "add a mutation", or asks to add a write/create/update/delete operation to an entity. Fire automatically when editing any file under `*/Application/**/Commands/*` or any file named `*Mutations.cs`. Bias toward firing — under-firing this skill yields hand-rolled input types, missing existence-leak guards, and tests that skip the mandatory authorization cases.
---

# command

Authors a complete write operation with Mocha: the Mocha mediator `ICommand` + `ICommandHandler` in the application layer, the HotChocolate mutation that calls it via `ISender.SendAsync`, and the command test class that covers the mandatory authorization cases. Use it whenever a user mutation, create/update/delete, or "command" comes up.

Mediator API reference (authoritative): the Mocha mediator docs under `mocha/v16/mediator/`.

## Mocha mediator vs. Mocha message bus

Mocha bundles two dispatch mechanisms. The **mediator** (`IMediator` / `ISender` / `IPublisher` in `Mocha.Mediator`) is for in-process CQRS within a single service. The **message bus** (`IMessageBus` in `Mocha`) is for cross-service / cross-process messaging. They share the same Roslyn source-generator infrastructure and middleware model but are separate primitives. This skill is about the in-process mediator. Commands live inside your application assembly and dispatch through `Mocha.Mediator`; cross-service events go on the message bus and are out of scope here.

## Why Mocha mediator, not MediatR

Mocha keeps the in-process CQRS surface (mediator) and the cross-service messaging surface (bus) under one set of middleware, instrumentation, and source-generator tooling. The mediator dispatches with no reflection — handlers are discovered and wired up at compile time by the Mocha Roslyn analyzer.

## What you produce

For a single write operation, three files (test path varies by module, see [references/TESTS.md](references/TESTS.md)):

```
src/MyApp.Application/Books/Commands/CreateBookCommand.cs
src/MyApp.GraphQL/Books/BookMutations.cs                       (edit, add method)
test/MyApp.Application.Tests/Books/Commands/CreateBookCommandTests.cs
```

## Command record

`User` (a `ClaimsPrincipal`) is always the first parameter. Use a sealed record. The result type is the entity (or a wrapper when there is something like a one-shot secret to surface).

```csharp
using System.Security.Claims;
using Mocha.Mediator;

public sealed record CreateBookCommand(ClaimsPrincipal User, Guid AuthorId, string Title)
    : ICommand<Book>;
```

Why a record: value equality, init-only, deconstruction in the handler. Why `sealed`: the source generator routes on the runtime type — no subtypes.

For void commands (no payload to return), implement the non-generic `ICommand`:

```csharp
public sealed record DeleteBookByIdCommand(ClaimsPrincipal User, Guid BookId) : ICommand;
```

Mocha exposes a `Unit` type for compatibility, but for void writes prefer the non-generic `ICommand` interface — it's shorter and matches every other void command.

## Handler shape

Inject `IPromiseCache`, your `DbContext` (or context abstraction), `IValidator<TEntity>`, and `IAuthorizationService`. Do not inject DataLoaders into command handlers — load through `context` with `Include` so you can save in the same unit of work, then `cache.Publish(...)` so downstream resolvers in the same request hit the cache.

```csharp
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

public sealed class CreateBookCommandHandler(
    IPromiseCache cache,
    IAppDbContext context,
    IValidator<Book> validator,
    IAuthorizationService authorization
) : ICommandHandler<CreateBookCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        CreateBookCommand request,
        CancellationToken cancellationToken)
    {
        var (user, authorId, title) = request;

        // 1. Authentication gate
        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        // 2. Load parent entity required for the permission check (single query, Include parents)
        var author = await context.Authors
            .FirstOrDefaultAsync(x => x.Id == authorId, cancellationToken);

        if (author is null)
        {
            throw new AuthorNotFoundException(authorId);
        }
        cache.Publish(author);

        // 3. Permission check — on failure throw the SAME error as not-found
        var auth = await authorization.AuthorizeAsync(user, author, "Books.Create");
        if (!auth.Succeeded)
        {
            throw new AuthorNotFoundException(authorId);
        }

        // 4. Build the entity through a domain factory, then validate
        var book = Book.Create(author.Id, title);
        await validator.ValidateAndThrowAsync(book, cancellationToken);

        // 5. Persist + publish + save
        context.Books.Add(book);
        cache.Publish(book);
        await context.SaveChangesAsync(cancellationToken);

        return book;
    }
}
```

`AuthorNotFoundException` here stands in for your project's own not-found exception type. The policy string (`"Books.Create"`) is whatever your authorization stack defines — `IAuthorizationService` resolves it against your registered policies or requirement handlers.

Step order is part of the contract — tests assert on it (see [references/TESTS.md](references/TESTS.md), "Test execution order").

### Handler signature rules

- The method is **`HandleAsync`**, not `Handle`.
- The return type is **`ValueTask<TResponse>`** (or `ValueTask` for void commands). Never `Task` — Mocha's pipeline expects `ValueTask`.
- `CancellationToken` is the last parameter and is required (no default value).
- Throwing is fine; `null` returns from a command handler are illegal — Mocha will throw `InvalidOperationException`. If "nothing to return" is a real state, model it with a wrapper type, not a nullable response.

### Existence-leak guard (security)

When the **permission** check fails on an entity that **does exist**, throw the parent's not-found exception, not `UnauthorizedAccessException`. Throwing an authorization error confirms the entity exists; throwing the not-found type is indistinguishable from "id is garbage". This is what makes random-id probing unprofitable for unauthorized users.

Right:

```csharp
var auth = await authorization.AuthorizeAsync(user, author, "Books.Create");
if (!auth.Succeeded)
{
    throw new AuthorNotFoundException(authorId);   // same error as the not-found branch above
}
```

Wrong:

```csharp
var auth = await authorization.AuthorizeAsync(user, author, "Books.Create");
if (!auth.Succeeded)
{
    throw new UnauthorizedAccessException();   // leaks existence of `authorId`
}
```

`UnauthorizedAccessException` is reserved for the **authentication** gate (no authenticated user at all). Once the user is authenticated but lacks access, behave as if the entity is not there.

### Domain methods, not setters

To create or change an entity, use its domain factory or mutator. Never assign properties directly inside a handler.

Right:

```csharp
var book = Book.Create(author.Id, title);
// or:
book.Rename(newTitle);
```

Wrong:

```csharp
var book = new Book { AuthorId = author.Id, Title = title };   // bypasses invariants and domain events
book.Title = newTitle;                                          // ditto
```

The domain enforces invariants (length, required fields, audit fields, raised domain events) inside those methods. A handler that reaches in with property assignment will silently violate them.

### Why `cache.Publish` on every loaded entity

The application is request-scoped around an `IPromiseCache` (from Green Donut). Subsequent resolvers in the same GraphQL request route through DataLoaders, which check the cache first. Publish every entity you've loaded in the handler (`author`, the newly created `book`) so the mutation's response selection set hits the cache instead of round-tripping.

### `IValidator<T>` + FluentValidation

Validate the **constructed entity**, not the request DTO. The validator lives in the domain or application layer and knows the invariants (regex, length, uniqueness). Always `ValidateAndThrowAsync`.

```csharp
await validator.ValidateAndThrowAsync(book, cancellationToken);
```

This is the only place a `ValidationException` should originate inside a handler.

## DI registration

Handlers are picked up by the Mocha Roslyn source generator. Register the module once in your composition root:

```csharp
services
    .AddMediator()
    .AddApplication();   // source-generated from the assembly name
```

The generated `Add{Module}()` extension method is named after the last segment of the assembly name (e.g. `MyApp.Application` → `AddApplication()`). To override, add `[assembly: MediatorModule("Name")]` to any file in the project.

Do not register individual handlers manually — the source generator builds the pre-compiled pipeline for every `ICommandHandler<,>`, `IQueryHandler<,>`, and `INotificationHandler<>` in the assembly. `AddHandler<T>()` exists as an escape hatch for plugin assemblies and integration tests, but the canonical path is the generated module.

## GraphQL mutation wrapper

For the full mutation file pattern, see [references/GRAPHQL-WRAPPER.md](references/GRAPHQL-WRAPPER.md). Quick reference:

```csharp
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types.Relay;
using Mocha.Mediator;

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

Inject `ISender` (not `IMediator`) when you only dispatch commands and queries. `IMediator : ISender, IPublisher` — pick the narrowest surface at the call site.

Three things this lays a trap for:

1. **Mutation Conventions wrap parameters automatically.** Method parameters become `CreateBookInput { authorId, title }`; the return becomes `CreateBookPayload`. Do not hand-roll an input class.

   Right: direct method parameters as above.

   Wrong:

   ```csharp
   public record CreateBookInput([ID<Author>] Guid AuthorId, string Title);

   public async Task<Book> CreateBookAsync(CreateBookInput input, ISender sender, ...)
   ```

2. **Use the generic `[Error<T>]` attribute.** It carries the type into the source generator and HotChocolate's schema emission. The non-generic `[Error(typeof(T))]` form loses that type information at compile time and the generated payload union ends up missing the error branch.

   Right: `[Error<AuthorNotFoundException>]`

   Wrong: `[Error(typeof(AuthorNotFoundException))]`

3. **`[ID]` annotates ID parameters.** Use `[ID(nameof(Author))]` or `[ID<Author>]` so HotChocolate decodes the Relay global ID before your method runs. Bare `Guid` parameters get treated as opaque UUIDs.

### Wrong vs right (mediator surface)

```csharp
// WRONG — MediatR types and Task<T> return
public sealed record CreateBookCommand(ClaimsPrincipal User, Guid AuthorId, string Title)
    : MediatR.IRequest<Book>;

public sealed class CreateBookCommandHandler
    : MediatR.IRequestHandler<CreateBookCommand, Book>
{
    public async Task<Book> Handle(CreateBookCommand request, CancellationToken ct)
    {
        // ...
    }
}
```

```csharp
// RIGHT — Mocha mediator types, ValueTask<T>, HandleAsync
using Mocha.Mediator;

public sealed record CreateBookCommand(ClaimsPrincipal User, Guid AuthorId, string Title)
    : ICommand<Book>;

public sealed class CreateBookCommandHandler
    : ICommandHandler<CreateBookCommand, Book>
{
    public async ValueTask<Book> HandleAsync(CreateBookCommand request, CancellationToken ct)
    {
        // ...
    }
}
```

```csharp
// WRONG — IMediator.Send (MediatR shape) in the mutation
public async Task<Book> CreateBookAsync(
    ClaimsPrincipal user, IMediator mediator, [ID(nameof(Author))] Guid authorId, string title, CancellationToken ct)
{
    return await mediator.Send(new CreateBookCommand(user, authorId, title), ct);
}
```

```csharp
// RIGHT — ISender.SendAsync
public async Task<Book> CreateBookAsync(
    ClaimsPrincipal user, ISender sender, [ID(nameof(Author))] Guid authorId, string title, CancellationToken ct)
{
    return await sender.SendAsync(new CreateBookCommand(user, authorId, title), ct);
}
```

## Tests

Five mandatory cases, in this order (matches the handler's execution flow):

1. `HandleAsync_ShouldCreate<Entity>_When<HappyPath>`
2. `HandleAsync_ShouldThrowUnauthorizedAccessException_WhenNotAuthenticated`
3. `HandleAsync_ShouldThrow<ParentNotFound>_WhenParentDoesNotExist`
4. `HandleAsync_ShouldThrow<ParentNotFound>_WhenUserLacksPermission` (existence-leak guard)
5. `HandleAsync_ShouldThrowValidationException_When<InputInvalid>`

Strict naming: `Method_Should<Outcome>_When<Condition>`. Single underscore between the three sections, no underscores inside them. Use vanilla xUnit (or NUnit) with a real database where the test demands it (e.g. `Microsoft.EntityFrameworkCore.InMemory` for fast tests, Testcontainers Postgres for integration). Full worked example with all five cases in [references/TESTS.md](references/TESTS.md).

## Gotchas

- **Permission-denied throws the not-found exception, not `UnauthorizedAccessException`.** This is the existence-leak guard. `UnauthorizedAccessException` is the *unauthenticated* path only. Tests assert both branches throw `<Parent>NotFoundException`, not different errors — if your test for "no permission" expects `UnauthorizedAccessException`, you have written the test against the wrong contract.
- **`cache.Publish` is not optional.** Skipping it works in isolated tests but causes a redundant DB hit in the live GraphQL request, because the response field resolvers re-fetch the entity through DataLoaders.
- **Validate the entity, not the request.** The `IValidator<TEntity>` runs against the constructed domain object so it sees the same invariants the entity enforces at runtime.
- **Don't mix mutation conventions with hand-rolled inputs.** If you create a `CreateBookInput` record, the source generator emits a doubly-wrapped `CreateBookInputInput`. The agent will write the wrapper unprompted unless told not to — read the GraphQL reference.
- **`ValueTask`, not `Task`.** Mocha's pipeline is `ValueTask`-based end-to-end. Returning `Task<T>` from `HandleAsync` will not compile against `ICommandHandler<,>`.
- **`HandleAsync`, not `Handle`.** MediatR-era handlers used `Handle`; Mocha uses `HandleAsync`.
- **`null` is not a valid response.** A handler that returns `null` from `ValueTask<T>` will trip Mocha at dispatch time. Throw an exception or return a non-null value.
- **`ISender.SendAsync` for commands, `ISender.QueryAsync` for queries.** They're distinct methods. `SendAsync(IQuery<T>)` is a compile error and vice versa.
- **Authentication check via `ClaimsPrincipal.Identity`.** `user.Identity is not { IsAuthenticated: true }` is the canonical guard. Don't reach for project-specific session shims when `ClaimsPrincipal` is right there.
- **Don't inject DataLoaders into command handlers.** Use the `DbContext` with `Include` so the loaded entities are tracked and saved in the same unit of work.

## Examples

**Example — update command (the second canonical shape):**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

public sealed record UpdateBookTitleCommand(
    ClaimsPrincipal User,
    Guid BookId,
    string NewTitle)
    : ICommand<Book>;

public sealed class UpdateBookTitleCommandHandler(
    IAppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<UpdateBookTitleCommand, Book>
{
    public async ValueTask<Book> HandleAsync(UpdateBookTitleCommand request, CancellationToken ct)
    {
        var (user, bookId, newTitle) = request;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var book = await context.Books.FirstOrDefaultAsync(x => x.Id == bookId, ct);
        if (book is null)
        {
            throw new BookNotFoundException(bookId);
        }
        cache.Publish(book);

        var auth = await authorization.AuthorizeAsync(user, book, "Books.Update");
        if (!auth.Succeeded)
        {
            throw new BookNotFoundException(bookId);
        }

        book.Rename(newTitle);

        await context.SaveChangesAsync(ct);
        return book;
    }
}
```

Notice: updates don't need a validator if the domain method enforces invariants; they still publish to the cache and still treat the permission denial as a not-found.

## References

- [TESTS.md](references/TESTS.md) — full command test class with the five mandatory cases, ordering rules, and how to wire up a vanilla xUnit test.
- [GRAPHQL-WRAPPER.md](references/GRAPHQL-WRAPPER.md) — mutation conventions, `[Error<T>]` vs `[Error(typeof(T))]`, `[ID]` variants, payloads, and the wrapper for commands that need a post-mutation re-read.
- Mocha mediator docs under `mocha/v16/mediator/` (`index.md` and `pipeline-and-middleware.md`).
