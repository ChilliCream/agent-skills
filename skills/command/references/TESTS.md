# Command tests

Detailed test patterns for Mocha mediator command handlers. The main skill body covers the rules; this file shows the full worked example with every mandatory case.

## Test project layout

Mirror the source layout under the test project: `Books/Commands/CreateBookCommandTests.cs` for a command at `Books/Commands/CreateBookCommand.cs`. Use one test project per application assembly (e.g. `MyApp.Application.Tests` for `MyApp.Application`).

## Test class shape

Plain xUnit (or NUnit) class. No mandatory base class — wire DI yourself or use the helpers your project already has. The example below uses a thin `CommandTestFixture` that sets up an in-memory database, the Mocha mediator, and `IPromiseCache`. Substitute your own.

```csharp
public sealed class CreateBookCommandTests : IAsyncLifetime
{
    private readonly CommandTestFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    private ISender Sender => _fixture.Services.GetRequiredService<ISender>();
    private IAppDbContext CreateContext() => _fixture.CreateContext();
}
```

What your fixture should expose:

- An `IServiceProvider` with `AddMediator().AddApplication()` registered, plus your `DbContext`, `IPromiseCache`, validators, and an `IAuthorizationService` (often a fake or test policy provider).
- A way to seed entities into the database — a builder, a factory, or just `context.Add(...)` in the `Arrange`.
- A way to construct an authenticated `ClaimsPrincipal` for a test user, and an anonymous `ClaimsPrincipal` for the unauthenticated case.
- A `CancellationToken` (often `TestContext.Current.CancellationToken` on xUnit v3, or just `default`).

The exact shape doesn't matter; what matters is that the test uses **`ISender`** to dispatch and asserts on the real handler output.

## Mandatory cases (in order)

Tests must follow the handler's execution flow. The five cases below cover every branch before the happy path's tail:

1. Happy path — proves the command persists and returns the entity.
2. Not authenticated — proves the first gate trips.
3. Parent does not exist — proves the not-found path.
4. User lacks permission — proves the existence-leak guard (same error as #3).
5. Validation fails — proves the validator runs after auth.

Why this order: a reader of the test file should be able to trace the handler top to bottom by reading the tests top to bottom. Mismatches mean either the tests are wrong, the order is wrong, or someone changed the handler without updating the tests.

## Full worked example

Test class for `CreateBookCommand`. The helpers (`SeedAuthor`, `AuthenticatedUser`, `AnonymousUser`, `GrantPermission`) are stand-ins — wire them through whatever your project uses.

```csharp
using System.Security.Claims;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Mediator;
using Xunit;

namespace MyApp.Application.Tests.Books;

public sealed class CreateBookCommandTests : IAsyncLifetime
{
    private readonly CommandTestFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    private ISender Sender => _fixture.Services.GetRequiredService<ISender>();
    private CancellationToken CT => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldCreateBook_WhenValidRequest()
    {
        // Arrange
        var author = await _fixture.SeedAuthor("J. Doe");
        var user = _fixture.AuthenticatedUser();
        _fixture.GrantPermission(user, author, "Books.Create");

        var command = new CreateBookCommand(user, author.Id, "Test Book");

        // Act
        var result = await Sender.SendAsync(command, CT);

        // Assert
        Assert.Equal("Test Book", result.Title);
        Assert.Equal(author.Id, result.AuthorId);

        await using var context = _fixture.CreateContext();
        var persisted = await context.Books.FindAsync([result.Id], CT);
        Assert.NotNull(persisted);
        Assert.Equal("Test Book", persisted.Title);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowUnauthorizedAccessException_WhenNotAuthenticated()
    {
        // Arrange
        var author = await _fixture.SeedAuthor("J. Doe");
        var command = new CreateBookCommand(_fixture.AnonymousUser(), author.Id, "Test Book");

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await Sender.SendAsync(command, CT));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowAuthorNotFoundException_WhenAuthorDoesNotExist()
    {
        // Arrange
        var missingAuthorId = Guid.NewGuid();
        var user = _fixture.AuthenticatedUser();
        var command = new CreateBookCommand(user, missingAuthorId, "Test Book");

        // Act + Assert
        var ex = await Assert.ThrowsAsync<AuthorNotFoundException>(
            async () => await Sender.SendAsync(command, CT));
        Assert.Equal(missingAuthorId, ex.AuthorId);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowAuthorNotFoundException_WhenUserLacksPermission()
    {
        // Arrange — author exists, user is authenticated, but no Books.Create permission
        var author = await _fixture.SeedAuthor("J. Doe");
        var user = _fixture.AuthenticatedUser();
        // NOTE: intentionally NOT calling GrantPermission

        var command = new CreateBookCommand(user, author.Id, "Test Book");

        // Act + Assert — existence-leak guard: same exception as "does not exist"
        var ex = await Assert.ThrowsAsync<AuthorNotFoundException>(
            async () => await Sender.SendAsync(command, CT));
        Assert.Equal(author.Id, ex.AuthorId);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenTitleIsEmpty()
    {
        // Arrange
        var author = await _fixture.SeedAuthor("J. Doe");
        var user = _fixture.AuthenticatedUser();
        _fixture.GrantPermission(user, author, "Books.Create");

        var command = new CreateBookCommand(user, author.Id, Title: "");

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await Sender.SendAsync(command, CT));
    }
}
```

### Dispatch returns `ValueTask` — wrap the call

`ISender.SendAsync` returns `ValueTask` or `ValueTask<T>`. `Assert.ThrowsAsync` wants a `Task`-returning lambda. The simplest way is an `async () => await Sender.SendAsync(...)` lambda — the compiler turns the `ValueTask` into a `Task` for the assertion.

If you prefer the call-without-`await` shape, materialize with `.AsTask()`:

```csharp
await Assert.ThrowsAsync<UnauthorizedAccessException>(
    () => Sender.SendAsync(command, CT).AsTask());
```

Both are fine. Pick one style per test class.

### Dispatching the handler directly is also fine

Unit tests that don't need the mediator pipeline can resolve the handler itself and call `HandleAsync` directly:

```csharp
var handler = _fixture.Services.GetRequiredService<CreateBookCommandHandler>();
await handler.HandleAsync(command, CT);
```

This skips middleware (validation logging, transaction wrapping, instrumentation). For most cases, dispatch through `Sender` so the test exercises the same path production does.

## Naming convention (strict)

`MethodName_Should<Outcome>_When<Condition>`.

- Method name for command handlers is **`HandleAsync`** (Mocha) — not `Handle`.
- Single underscore between the three sections.
- camelCase inside `Should...` and `When...` — no underscores there.

Right:

- `HandleAsync_ShouldCreateBook_WhenValidRequest`
- `HandleAsync_ShouldThrowAuthorNotFoundException_WhenUserLacksPermission`

Wrong:

- `HandleAsync_Should_Create_Book_When_Valid_Request` (extra underscores)
- `Handle_ShouldCreateBook_WhenValidRequest` (MediatR-era name; Mocha is `HandleAsync`)
- `CreateBook_ShouldSucceed_WhenValidInput` (uses the verb, not `HandleAsync`)
- `Should_Create_Book` (missing method name and the `When` half)

## Seeding tips

- Reuse stable ids across a single test (e.g. `var author = await _fixture.SeedAuthor(...)`); don't sprinkle `Guid.NewGuid()` inside the assertions.
- For "does not exist" tests, generate a fresh `Guid.NewGuid()` and assert the exception carries that id back.
- Build entities through their domain factories (`Author.Create(...)`) inside the seeder, not by `new Author { ... }`. Same rule as in the handler.
- Real database in integration tests where it matters. `EntityFrameworkCore.InMemory` is fine for fast tests that don't depend on relational behavior; switch to Testcontainers Postgres (or your real DB) for anything that does.

## Where to put `Assert.Equal` on the thrown exception

Your project's not-found exceptions should carry the offending id as a property. Assert it.

```csharp
var ex = await Assert.ThrowsAsync<AuthorNotFoundException>(
    async () => await Sender.SendAsync(command, CT));
Assert.Equal(author.Id, ex.AuthorId);
```

This catches a subtle bug class: handler throws the right exception type but with the wrong id (e.g., it threw `new AuthorNotFoundException(Guid.Empty)`).

## Optional extra cases

Beyond the five mandatory ones, add cases for:

- **`HandleAsync_Should<DoX>_When<DomainConditionY>`** — domain-specific branches (e.g., update succeeds when only one of several optional fields is set).
- **Cross-tenant access** — a user authenticated as someone else tries to mutate an entity in a tenant they don't belong to. Must throw the not-found exception, same as a missing entity.
- **Alternative principals** — API keys, service accounts, etc., dispatched as a `ClaimsPrincipal` populated with the relevant claims.

## Common test mistakes

- **Asserting `UnauthorizedAccessException` for permission denial.** Wrong — handler throws the not-found exception to avoid leaking existence. The test should expect `<Parent>NotFoundException`.
- **Manual `DbContext` seeding that bypasses domain factories.** Don't `context.Authors.Add(new Author { ... })` in a test. Go through `Author.Create(...)` so invariants and domain events fire the same way as in production.
- **`Assert.NotNull(result)` alone.** Not a test. Assert the fields you care about (`result.Title`, `result.AuthorId`) and re-read from a fresh context to prove persistence.
- **Calling `Mediator.Send(...)` from MediatR muscle memory.** That's the wrong API. Use `Sender.SendAsync(...)` (Mocha). Same for queries — `Sender.QueryAsync(...)`, not `Send`.
- **Awaiting `ValueTask` twice.** `await Sender.SendAsync(cmd, CT)` once. Don't store it in a variable and await it again.
- **No filter when iterating.** Run a single class while iterating: `dotnet test --filter "FullyQualifiedName~CreateBookCommandTests"`. Never the full suite for one command.
- **Out-of-order tests.** Test methods should appear in the same order as the handler's branches (auth → not-found → permission → validation → happy path side effects). Re-orderable test runners notwithstanding, the *file* order is documentation.
