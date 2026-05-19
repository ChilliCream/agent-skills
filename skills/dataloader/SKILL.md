---
name: dataloader
description: Author Green Donut DataLoaders using the source generator ([DataLoader] attribute, partial methods). Fire whenever the user mentions "DataLoader", "[DataLoader]", "batched loading", "Green Donut", "GraphQL batching", "Dictionary<TKey, TValue> DataLoader", "ToBatchPageAsync", "ILookup<", "DataLoaderGroup", or "BatchingContext", whenever a new GraphQL field needs to resolve a related entity, and whenever editing or creating any file matching `*DataLoaders.cs`. Bias toward firing — this is the only correct shape for batched reads.
---

# dataloader

Write Green Donut DataLoaders that the source generator turns into batched, request-scoped resolvers. The wrong shape compiles but fails silently — generated interface names depend on the method name, and missing `AsNoTracking()` leaks change tracking across requests. Follow the rules below verbatim.

## When to use

Use whenever you add a new DataLoader, edit a `*DataLoaders.cs` file, or add a GraphQL field that resolves from a related entity. If you are loading an entity by id in a query handler or a resolver, the answer is "go through a DataLoader" — never `context.Set<T>().FirstOrDefaultAsync(...)` from a resolver.

## What good looks like

A complete entity DataLoader file. Every block is annotated with the rule it enforces.

```csharp
using GreenDonut.Data;

namespace MyApp.Application.Books;

// Wrap the static class with [DataLoaderGroup("<Entity>BatchingContext")].
// The generator emits an interface I<Entity>BatchingContext exposing each
// DataLoader as a property. Consumers inject the context, not individual
// DataLoaders — one constructor parameter, many lookups available.
[DataLoaderGroup("BookBatchingContext")]
public static class BookDataLoaders
{
    // ── Single-key, lookup-aware ────────────────────────────────────────
    // Method name drives the generated interface: GetBookByIdAsync →
    // IBookByIdDataLoader. Keep the entity name singular.
    //
    // Lookups = [nameof(GetBookByIdLookup)] wires this loader to the
    // promise cache. If another resolver already published a Book via
    // cache.Publish(book), the loader returns it without a DB round-trip.
    [DataLoader(Lookups = [nameof(GetBookByIdLookup)])]
    public static async Task<Dictionary<Guid, Book>> GetBookByIdAsync(
        IReadOnlyList<Guid> keys,          // batch of keys, never a single id
        IAppDbContext context,              // injected by the generator
        CancellationToken cancellationToken) // required, no default value
    {
        return await context
            .Books
            .AsNoTracking()                 // reads are read-only; tracking
                                            // wastes memory and risks bleeding
                                            // mutation state across requests
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
            // Return type is Dictionary<TKey, TValue> — not
            // IReadOnlyDictionary<,>. The generator requires the concrete type.
    }

    // Lookup function: tells the cache how to extract the key from a
    // cached Book. Public static, matches the type signature
    // Func<TValue, TKey>.
    public static Guid GetBookByIdLookup(Book book) => book.Id;

    // ── Single-key for a different entity ───────────────────────────────
    // GetAuthorByIdAsync → IAuthorByIdDataLoader. One file per primary
    // entity is the norm, but related entities frequently sit alongside.
    [DataLoader(Lookups = [nameof(GetAuthorByIdLookup)])]
    public static async Task<Dictionary<Guid, Author>> GetAuthorByIdAsync(
        IReadOnlyList<Guid> keys,
        IAppDbContext context,
        CancellationToken cancellationToken)
    {
        return await context
            .Authors
            .AsNoTracking()
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static Guid GetAuthorByIdLookup(Author author) => author.Id;

    // ── One-to-many, non-paginated (ILookup<,>) ────────────────────────
    // Use ILookup<TKey, TValue> when one key has many values and you do
    // not need paging — typical for projections / small relation sets.
    // GetBooksByAuthorIdAsync → IBooksByAuthorIdDataLoader.
    [DataLoader]
    public static async Task<ILookup<Guid, Book>> GetBooksByAuthorIdAsync(
        IReadOnlyList<Guid> keys,
        IAppDbContext context,
        CancellationToken cancellationToken)
    {
        var result = await context.Books
            .AsNoTracking()
            .Where(x => keys.Contains(x.AuthorId))
            .ToListAsync(cancellationToken: cancellationToken);

        return result.ToLookup(x => x.AuthorId);
    }

    // ── Paginated (Page<T>) ─────────────────────────────────────────────
    // Prefix the method with Page... (not Get...) when the result is paged.
    // PageBooksByAuthorIdAsync → IBooksByAuthorIdDataLoader (the Page prefix
    // is stripped by the generator). Sort deterministically: a secondary
    // ThenBy(x => x.Id) prevents cursor drift on equal primary sort keys.
    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Book>>> PageBooksByAuthorIdAsync(
        IReadOnlyList<Guid> keys,
        IAppDbContext context,
        PagingArguments arguments,          // injected by the generator
        CancellationToken cancellationToken)
    {
        return await context.Books
            .AsNoTracking()
            .Where(x => keys.Contains(x.AuthorId))
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .ToBatchPageAsync(
                x => x.AuthorId,            // key selector groups rows per key
                arguments,
                cancellationToken);
    }
}
```

### What every block is doing

- **`[DataLoaderGroup("<Entity>BatchingContext")]`** — generates `I<Entity>BatchingContext` so callers inject one interface and reach every loader as a property. Use one group per `*DataLoaders.cs` file.
- **`[DataLoader]`** — turns the static method into a request-scoped, batched loader. The generator produces both the implementation and an interface named from the method.
- **`Lookups = [nameof(GetXxxId)]`** — wires the loader to the `IPromiseCache`. When a command publishes an entity (`cache.Publish(book)`), subsequent loads inside the same request resolve from cache. Without lookups you re-query the database on every load.
- **`IReadOnlyList<TKey> keys`** — the batch. Always plural, even when the consumer asks for a single id; Green Donut coalesces concurrent `LoadAsync` calls into one batch.
- **`AsNoTracking()`** — required. Loaders return read-only data; EF change tracking is wasted work and bleeds entity state across requests on shared contexts.
- **`Dictionary<TKey, TValue>`** — the concrete type, not `IReadOnlyDictionary<,>`. The generator's contract is strict.
- **`CancellationToken cancellationToken`** — required, no default. A default value masks accidental fire-and-forget calls.

## What bad looks like

Each `// ❌ WRONG` line is a real failure mode the source generator or runtime will not catch for you.

```csharp
public static class BookDataLoaders
{
    // ❌ WRONG: Does not use lookups for data loaders
    // ❌ WRONG: Does not return a Dictionary<,>; returns an IReadOnlyDictionary<,>
    // ❌ WRONG: The name is GetBooksByIdsAsync instead of GetBookByIdAsync
    public static async Task<IReadOnlyDictionary<Guid, Book>> GetBooksByIdsAsync(
        IReadOnlyList<Guid> ids,
        IAppDbContext context,
        // ❌ WRONG: Do not provide a default value
        CancellationToken cancellationToken = default)
    {
        return await context.Books
            // ❌ WRONG: Does not use AsNoTracking
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }
}
```

Why each one matters:

- **Plural method name (`GetBooksByIdsAsync`)** generates `IBooksByIdsDataLoader`. Every consumer in the codebase expects `IBookByIdDataLoader`. The build either fails or, worse, succeeds with a parallel interface that nothing wires up.
- **`IReadOnlyDictionary<,>`** — the generator templates against `Dictionary<,>`. Different return type, no generated implementation.
- **Missing `Lookups`** — every load round-trips the DB even after a command has just published the entity. Performance regression hides until you read query logs.
- **`AsNoTracking()` missing** — change tracking leaks across requests on a shared `IAppDbContext`, eventually surfacing as `InvalidOperationException` ("instance is already being tracked") in unrelated handlers.
- **`CancellationToken cancellationToken = default`** — silently swallows cancellation when a caller forgets to pass the token.

## Naming rules (strict — derived from the method name)

The generator is mechanical. The method name **is** the public contract.

| Method | Generated interface | Use case |
|---|---|---|
| `GetBookByIdAsync` | `IBookByIdDataLoader` | one key → one value |
| `GetBookByIsbnAsync` | `IBookByIsbnDataLoader` | alternate single key |
| `GetBooksByAuthorIdAsync` | `IBooksByAuthorIdDataLoader` | one key → many values (`ILookup<,>`) |
| `PageBooksByAuthorIdAsync` | `IBooksByAuthorIdDataLoader` | one key → page of values |

Rules:

- Singular entity for one-to-one (`GetBookByIdAsync`, not `GetBooksByIdAsync`).
- `Page` prefix for `Dictionary<TKey, Page<TValue>>`; the generator strips it.
- `By<Key>` describes the lookup key. Multiple keys per entity are fine — `GetBookByIdAsync` and `GetBookByIsbnAsync` coexist.

## File location

```
src/MyApp.Application/
  Books/
    DataLoaders/
      BookDataLoaders.cs
```

One `*DataLoaders.cs` per entity. One `[DataLoaderGroup("<Entity>BatchingContext")]` per file.

## Method shapes

| Shape | Signature |
|---|---|
| One-to-one | `Task<Dictionary<TKey, TValue>>` |
| One-to-many, paged | `Task<Dictionary<TKey, Page<TValue>>>` with `PagingArguments` parameter |
| One-to-many, non-paged | `Task<ILookup<TKey, TValue>>` |

Standard parameter order: `IReadOnlyList<TKey> keys`, optional `PagingArguments arguments`, `IAppDbContext context`, `CancellationToken cancellationToken`.

## EF Core checklist

- `.AsNoTracking()` — always.
- `.Where(x => keys.Contains(x.<Key>))` — translates to `WHERE x.<Key> = ANY(@keys)`.
- `.ToDictionaryAsync(x => x.<Key>, ct)` for one-to-one.
- `.ToBatchPageAsync(x => x.<Key>, arguments, ct)` for paged. Requires deterministic `OrderBy(...).ThenBy(x => x.Id)`.
- For one-to-many non-paged, materialize with `ToListAsync` then `.ToLookup(x => x.<Key>)`.

## Lookups — when and why

Add `Lookups = [nameof(GetXxxId)]` whenever the loaded entity may already be in the `IPromiseCache`. That includes:

- Anything a command publishes via `cache.Publish(entity)`.
- Anything resolved by another DataLoader earlier in the same request.

Skip lookups only for projections or entities that are never published.

The lookup function is a `public static` method matching `Func<TValue, TKey>`:

```csharp
public static Guid GetBookByIdLookup(Book book) => book.Id;
```

Multiple lookups are allowed — `Lookups = [nameof(GetBookByIdLookup), nameof(GetBookByIsbnLookup)]` — when an entity is cached under more than one key.

## Consuming the DataLoader

Inject the generated batching context (preferred — one parameter covers every loader the entity exposes):

```csharp
public sealed class BookResolvers
{
    public async Task<Book?> GetBookAsync(
        Guid id,
        IBookBatchingContext books,        // generated from [DataLoaderGroup]
        CancellationToken cancellationToken)
        => await books.BookById.LoadAsync(id, cancellationToken);
}
```

Or inject the individual DataLoader interface when the consumer only needs one:

```csharp
public sealed class GetBookById(
    IBookByIdDataLoader bookById,
    IAuthorizationService authorizationService)
{
    public async Task<Book?> ExecuteAsync(
        ISession session,
        Guid id,
        CancellationToken cancellationToken)
    {
        var book = await bookById.LoadAsync(id, cancellationToken);
        // ... authorization check ...
        return book;
    }
}
```

Both forms are correct. Use the batching context when a class touches several loaders for the same entity; use the single interface when it touches one.

### Paged consumption

`Page<T>` loaders take `PagingArguments` alongside the key:

```csharp
var page = await books.BooksByAuthorId.LoadAsync(
    authorId,
    new PagingArguments { First = 20, After = cursor },
    cancellationToken);
```

## Gotchas

- **Method name = public API.** Renaming `GetBookByIdAsync` to `GetBookByIdentifierAsync` breaks every consumer because the generated interface name changes. Treat renames like a public-API change.
- **`Page` prefix is significant.** `GetBooksByAuthorIdAsync` returning `Dictionary<Guid, Page<Book>>` is a contradiction the generator may accept but consumers will misread. Use `PageBooksByAuthorIdAsync` when the result is paged, `GetBooksByAuthorIdAsync` only for non-paged returns.
- **Order before `ToBatchPageAsync`, deterministically.** A single `OrderBy(x => x.Title)` without a tiebreaker on `Id` produces unstable cursors when titles collide.
- **Use the same DI context across the file.** The generator wires `IAppDbContext`; mixing in another abstraction breaks DI.
- **Don't add business logic in the loader.** Filtering by tenant, soft-delete, or visibility belongs in the query/resolver layer with `IAuthorizationService`. The loader is a pure batch fetch.
- **Don't loop over keys.** If you find yourself writing `foreach (var key in keys) { ... }`, you are defeating batching. Express the fetch as one query with `Where(x => keys.Contains(...))`.
- **Lookup function must be `public static`.** Anything else and the generator silently skips it.
