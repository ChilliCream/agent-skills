---
name: snapshot-test
description: Write Snapshooter-based snapshot tests for HotChocolate GraphQL operations executed through a Strawberry Shake client. Fire whenever the user says "snapshot test", "Strawberry Shake test", "MatchSnapshot", "test this GraphQL operation", "test this mutation", "test this query", "GraphQL integration test", "add a test for this resolver/mutation/subscription", or "/snapshot-test"; or whenever the user is editing/creating a file under a `*.Tests/` project that exercises a HotChocolate query, mutation, or subscription via `IRequestExecutor` or a generated Strawberry Shake client. Prefer this over generic test scaffolding for anything that talks to the GraphQL layer — handwritten field-by-field assertions on GraphQL responses are wrong by default.
---

# snapshot-test

Write a GraphQL integration test that spins up a real HotChocolate `IRequestExecutor`, seeds the test data your operation needs, runs the operation through a Strawberry Shake client (or the raw executor), and asserts the result with `MatchSnapshot`. The shape of the response is the contract; snapshots make accidental schema drift impossible to merge.

Use this skill whenever you are adding or editing a test in a `*.Tests/` project that touches a HotChocolate operation. Do not write field-by-field assertions for GraphQL responses — snapshots catch shape changes that hand-written asserts miss, and field-by-field asserts go stale the moment someone adds a column.

## Instructions

### 1. Build the executor with your project's standard HotChocolate test fixture

Use whatever fixture/builder your project already exposes. The shape that comes out the other side is what matters: an `IRequestExecutor` (and, when you use Strawberry Shake, a generated client resolved from the same provider) with the schema your operation needs. A vanilla setup looks like this:

```csharp
var services = new ServiceCollection();
services
    .AddSingleton<IBookRepository, InMemoryBookRepository>()
    .AddGraphQL()
    .AddQueryType<BookQueries>()
    .AddMutationType<BookMutations>()
    .AddType<BookType>();

await using var provider = services.BuildServiceProvider();
var executor = await provider
    .GetRequiredService<IRequestExecutorResolver>()
    .GetRequestExecutorAsync();
```

When Strawberry Shake is the client, resolve the generated client from the same provider:

```csharp
var client = provider.GetRequiredService<IBookClient>();
```

Drop the authenticated-user registration (or swap to an anonymous principal) to author the "not authenticated" negative test. Swap to a different user/principal to author the "wrong tenant" test.

### 2. Seed data deterministically

Use whatever fluent seed helper your project ships (an `ISeeder`, a `TestDataBuilder`, an in-memory repository pre-populated in the fixture, etc.). Two rules:

1. Never hand-construct entities directly against a `DbContext` inside a test — push the shape into a reusable builder so every test agrees on what a "valid Author" looks like.
2. Use stable, well-known IDs (constants like `TestIds.Author1`) so the snapshot is deterministic across runs.

```csharp
await seed
    .AddAuthor(TestIds.Author1, name: "Ada Lovelace")
    .AddBook(TestIds.Book1, authorId: TestIds.Author1, title: "Notes on the Analytical Engine")
    .RunAsync();
```

Omitting a seed step (e.g. not seeding the author) is how you author the "not found" negative test.

### 3. Execute via the Strawberry Shake client

```csharp
var client = provider.GetRequiredService<IBookClient>();

var result = await client.CreateBook.ExecuteAsync(new CreateBookInput
{
    AuthorId = TestIds.Author1,
    Title = "A Sequel",
});
```

Use the generated input record exactly as Strawberry Shake exposes it. For raw-GraphQL tests (no generated client), use `OperationRequestBuilder` + `executor.ExecuteAsync(operation)` and snapshot `result.ExpectOperationResult().ToJson()`.

### 4. Assert with `MatchSnapshot` and ignore non-deterministic fields

```csharp
result.Data.MatchSnapshot(x => x
    .IgnoreField("**.Id")
    .IgnoreField("**.CreatedAt"));
```

Why: snapshots compare the rendered JSON verbatim. Random GUIDs, wall-clock timestamps, ephemeral URLs, and any field that changes per-run will make the test flake on every CI run if you don't ignore them. The `**.` glob matches the field at any depth.

Fields that almost always need ignoring:

- `**.Id`, `**.NodeId`, `**.Hash` — generated per test run.
- `**.CreatedAt`, `**.UpdatedAt`, `**.LastSeenAt`, anything ending in `At` — wall clock.
- `**.DownloadUrl`, signed URLs, presigned blob URLs.
- `**.Token`, `**.RefreshToken`, anything cryptographic.

When you legitimately need to assert on a specific value (e.g. that `Title == "A Sequel"`), keep one or two explicit `Assert.Equal` calls alongside `MatchSnapshot`. The snapshot covers shape, the explicit assert pins the semantic.

### 5. Cover the mandatory failure cases per operation

Every operation gets the same four shadow tests:

1. **Happy path** — full setup, expect data, snapshot the payload.
2. **Not authenticated** — drop the authenticated principal, expect the auth error code on `result.Errors[0]` and `result.MatchSnapshot()`.
3. **Not found** — full auth but omit the seed of the target entity, expect the typed not-found error.
4. **No permission / wrong tenant** — execute as a principal who doesn't own the entity, expect the same typed not-found error (deliberately leak nothing in error payloads).

Order the test methods in the source file to match the execution flow of the resolver: auth → not-found → permission → business logic variants. Reviewers reading the file should be able to walk the resolver and the tests in lockstep.

### 6. Test naming — strict

Pattern: `Method_Should<Outcome>_When<Condition>`. Single underscore between sections, camelCase inside each section, no extra underscores.

| OK | Not OK |
|---|---|
| `CreateBook_ShouldReturnBook_WhenInputIsValid` | `CreateBook_Should_Return_Book_When_InputIsValid` |
| `CreateBook_ShouldReturnAuthorNotFoundError_WhenAuthorIsMissing` | `CreateBookMethod_ShouldReturnError_When_AuthorIsMissing` |
| `GetBooks_ShouldReturnPagedList_WhenAuthorHasBooks` | `Should_Return_Books_When_Author_Has_Books` |

If you must deviate (e.g. the existing test class uses `Should_` with underscores), match the file's existing convention rather than mix styles. New files use the strict form.

### 7. Handle snapshot mismatches

When a snapshot test fails, Snapshooter writes the actual output to `__snapshots__/__mismatch__/` next to the original. Inspect the diff, then if correct copy the mismatch over the original and remove the `__mismatch__/` folder:

```bash
# from the test project root
cp __snapshots__/__mismatch__/<file>.snap __snapshots__/<file>.snap
rm -rf __snapshots__/__mismatch__/
```

Never blanket-accept without diffing — the whole point of the snapshot is to flag accidental shape changes. If the diff contains a previously-ignored non-deterministic field (a new `*.Id`, a new timestamp), the right fix is to extend the `IgnoreField` chain, not to bake the random value into the snapshot. See [references/MISMATCH-WORKFLOW.md](references/MISMATCH-WORKFLOW.md) for the full workflow including CI artifact retrieval.

## Examples

### Example A — mutation snapshot test

```csharp
public sealed class CreateBookTests
{
    [Fact]
    public async Task CreateBook_ShouldReturnBook_WhenInputIsValid()
    {
        // arrange
        await using var provider = TestServices.Build(); // your project's fixture
        await provider.SeedAsync(seed => seed
            .AddAuthor(TestIds.Author1, name: "Ada Lovelace"));

        var client = provider.GetRequiredService<IBookClient>();

        // act
        var result = await client.CreateBook.ExecuteAsync(new CreateBookInput
        {
            AuthorId = TestIds.Author1,
            Title = "Notes on the Analytical Engine",
        });

        // assert
        Assert.Null(result.Data?.CreateBook.Errors);
        Assert.Equal("Notes on the Analytical Engine", result.Data?.CreateBook.Book?.Title);
        result.Data.MatchSnapshot(x => x
            .IgnoreField("**.Id")
            .IgnoreField("**.CreatedAt"));
    }

    [Fact]
    public async Task CreateBook_ShouldReturnAuthError_WhenNotAuthenticated()
    {
        await using var provider = TestServices.BuildAnonymous(); // no authenticated principal
        await provider.SeedAsync(seed => seed
            .AddAuthor(TestIds.Author1, name: "Ada Lovelace"));

        var client = provider.GetRequiredService<IBookClient>();

        var result = await client.CreateBook.ExecuteAsync(new CreateBookInput
        {
            AuthorId = TestIds.Author1,
            Title = "A Sequel",
        });

        Assert.Equal("AUTH_NOT_AUTHENTICATED", result.Errors[0].Code);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task CreateBook_ShouldReturnAuthorNotFoundError_WhenAuthorIsMissing()
    {
        // note: no AddAuthor -> target entity does not exist
        await using var provider = TestServices.Build();

        var client = provider.GetRequiredService<IBookClient>();

        var result = await client.CreateBook.ExecuteAsync(new CreateBookInput
        {
            AuthorId = TestIds.Author1,
            Title = "A Sequel",
        });

        Assert.IsAssignableFrom<IAuthorNotFoundError>(
            Assert.Single(result.Data?.CreateBook.Errors!));
    }

    [Fact]
    public async Task CreateBook_ShouldReturnAuthorNotFoundError_WhenAuthorBelongsToOtherTenant()
    {
        // arrange — execute as another user who does NOT own this author
        await using var provider = TestServices.BuildAsOtherUser();
        await provider.SeedAsync(seed => seed
            .AddAuthor(TestIds.Author1, name: "Ada Lovelace"));

        var client = provider.GetRequiredService<IBookClient>();

        var result = await client.CreateBook.ExecuteAsync(new CreateBookInput
        {
            AuthorId = TestIds.Author1,
            Title = "A Sequel",
        });

        // we deliberately return "not found", not "forbidden", to avoid leaking existence
        Assert.IsAssignableFrom<IAuthorNotFoundError>(
            Assert.Single(result.Data?.CreateBook.Errors!));
    }
}
```

Notice: the four-test cluster (happy / not-authenticated / not-found / cross-tenant) is the contract for every mutation. The happy path snapshots the payload; the negatives use typed error assertions, with `MatchSnapshot()` only on the auth error where the error shape itself is the contract.

### Example B — query snapshot test using raw `executor.ExecuteAsync`

When there is no generated Strawberry Shake operation (or you want to exercise the raw GraphQL pipeline), build the operation manually and snapshot the JSON:

```csharp
public sealed class GetBooksTests
{
    [Fact]
    public async Task GetBooks_ShouldReturnPagedList_WhenAuthorHasBooks()
    {
        // arrange
        await using var provider = TestServices.Build();
        await provider.SeedAsync(seed => seed
            .AddAuthor(TestIds.Author1, name: "Ada Lovelace")
            .AddBook(TestIds.Book1, authorId: TestIds.Author1, title: "Notes on the Analytical Engine")
            .AddBook(TestIds.Book2, authorId: TestIds.Author1, title: "A Sequel"));

        var executor = await provider
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

        // act
        var operation = OperationRequestBuilder.New()
            .SetDocument(Operations.GetBooks)
            .SetVariableValues(new Dictionary<string, object?>
            {
                ["authorId"] = TestIds.Author1,
                ["first"] = 10,
            })
            .Build();

        var result = await executor.ExecuteAsync(operation);

        // assert
        result.ExpectOperationResult().ToJson().MatchSnapshot();
    }
}

file static class Operations
{
    public const string GetBooks = """
        query GetBooks($authorId: ID!, $first: Int!) {
          author(id: $authorId) {
            books(first: $first) {
              nodes { id title }
              pageInfo { hasNextPage endCursor }
            }
          }
        }
        """;
}
```

Notice: the GraphQL document lives in a `file static class` at the bottom — keep it near the test, not in a shared `.graphql` file, so a reader sees both at once.

### Wrong vs right — assertions on a GraphQL response

```csharp
// WRONG — every field hand-asserted. Stale the moment the schema gains a column.
Assert.NotNull(result.Data);
Assert.Equal("A Sequel", result.Data.CreateBook.Book.Title);
Assert.Equal(TestIds.Author1, result.Data.CreateBook.Book.AuthorId);
Assert.NotNull(result.Data.CreateBook.Book.CreatedAt);
Assert.NotNull(result.Data.CreateBook.Book.Id);
// ...20 more lines of this...
```

```csharp
// RIGHT — snapshot covers shape, explicit asserts pin the contractual values, ignores cover noise.
Assert.Null(result.Data?.CreateBook.Errors);
Assert.Equal("A Sequel", result.Data?.CreateBook.Book?.Title);
result.Data.MatchSnapshot(x => x
    .IgnoreField("**.Id")
    .IgnoreField("**.CreatedAt"));
```

## Gotchas

- **Don't ignore everything.** If your `IgnoreField` chain has 10 entries, the snapshot is no longer a contract. Snapshot a smaller projection (`result.Data.CreateBook.Book.Title`) or ignore only at the leaves you actually need to ignore.
- **Bake values into the snapshot, never into ignores.** If `Title` is "A Sequel" and that matters, assert it explicitly with `Assert.Equal`. Ignoring it via `**.Title` would hide a regression.
- **Snapshots are ordering-sensitive.** Collection ordering in EF queries is not guaranteed unless you `OrderBy`. If a snapshot flakes on order, fix the query, don't sort the snapshot.
- **One executor per test.** Don't share `IRequestExecutor` (or its provider) across `[Fact]`s — state bleeds between tests. Build a fresh provider per test.
- **Resolve the Strawberry Shake client from the same provider as the executor.** Otherwise the client talks to a different schema than the one under test, and the failure mode looks unrelated.
- **`__mismatch__` files are not committed.** They are local artifacts. Commit only the accepted `__snapshots__/*.snap` files. If you see `__mismatch__/` in `git status`, you have a failing snapshot to resolve before pushing.
- **Don't mock the database (when your project uses a real one).** Integration tests should run against the same store production uses (test containers, in-memory repository — whatever the project standardizes on). Mocking the data layer produces tests that pass against a fictional schema.
- **Cross-tenant tests use the typed not-found error, not "forbidden".** Returning the not-found error to a non-owner is intentional — it prevents existence enumeration. Don't "fix" the test to expect a forbidden code.
- **Run tests with `--filter`.** `dotnet test --filter "FullyQualifiedName~CreateBookTests"` while iterating. Full-suite runs are slow and burn CI minutes.

## References

- [MISMATCH-WORKFLOW.md](references/MISMATCH-WORKFLOW.md) — full Snapshooter mismatch flow, including CI artifact retrieval.
- HotChocolate testing docs — `IRequestExecutor`, `OperationRequestBuilder`.
- Strawberry Shake docs — generated client registration and execution.
- Snapshooter docs — `MatchSnapshot`, `IgnoreField` globs.
