# HotChocolate v16 — Framework-Specific Patterns

Quick reference for implementing GraphQL schemas with HotChocolate v16 (ChilliCream).
This supplements the framework-agnostic design references with implementation specifics.

Source: [ChilliCream v16 Docs](https://next.chillicream.com/docs/hotchocolate/v16)

---

## Type System / Source Generators

### Root Types

```csharp
[QueryType]
public static partial class BookQueries
{
    public static async Task<Book?> GetBookAsync(
        [ID] Guid id,
        BookService books,             // auto-injected (v16: no [Service] needed)
        CancellationToken ct)
        => await books.GetByIdAsync(id, ct);
}

[MutationType]
public static partial class BookMutations
{
    [Error(typeof(BookNotFoundError))]
    public static async Task<Book> UpdateBookTitleAsync(
        [ID] Guid bookId,
        string title,
        BookService books,
        CancellationToken ct)
        => await books.UpdateTitleAsync(bookId, title, ct);
}
```

Multiple classes with the same root type attribute merge automatically.

### Object Type Extensions

```csharp
[ExtendObjectType<Book>]
public static partial class BookExtensions
{
    // Adds computed field to Book type
    public static string GetDisplayTitle([Parent] Book book)
        => $"{book.Title} ({book.PublishedAt:yyyy})";

    // Replace foreign key with resolved entity
    [BindMember(nameof(Book.AuthorId))]
    public static async Task<Author?> GetAuthorAsync(
        [Parent] Book book,
        IAuthorByIdDataLoader loader,
        CancellationToken ct)
        => await loader.LoadAsync(book.AuthorId, ct);
}
```

### Field Configuration

```csharp
// Code-first descriptor for explicit control
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Field(f => f.Id).ID();           // Mark as Relay global ID
        descriptor.Ignore(f => f.InternalCode);      // Hide from schema
        descriptor.BindFieldsExplicitly();            // Only expose configured fields
    }
}
```

### Naming Conventions

- `GetBookByIdAsync` → `bookById` (strips `Get` prefix and `Async` suffix)
- Properties: `Title` → `title` (auto camelCase)
- Override with `[GraphQLName("customName")]`
- Hide with `[GraphQLIgnore]`

---

## Mutation Conventions (Stage 6a)

### Setup

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMutationConventions(applyToAllMutations: true);
```

### Auto-Generated Input/Payload

A mutation method:

```csharp
[UseMutationConvention]
public static async Task<Book?> UpdateBookTitleAsync(
    [ID] Guid bookId,
    string title,
    BookService books,          // excluded from input (service)
    CancellationToken ct)       // excluded from input
    => await books.UpdateTitleAsync(bookId, title, ct);
```

Generates:

```graphql
input UpdateBookTitleInput {
  bookId: ID!
  title: String!
}

type UpdateBookTitlePayload {
  book: Book
  errors: [UpdateBookTitleError!]
}
```

### Error Types (Stage 6a Pattern)

```csharp
[MutationType]
public static partial class BookMutations
{
    [Error(typeof(BookTitleTakenException))]
    [Error(typeof(InvalidBookTitleException))]
    public static async Task<Book?> UpdateBookTitleAsync(
        [ID] Guid bookId,
        string title,
        BookService books,
        CancellationToken ct)
        => await books.UpdateTitleAsync(bookId, title, ct);
}
```

Generated schema (Stage 6a):

```graphql
type UpdateBookTitlePayload {
  book: Book
  errors: [UpdateBookTitleError!]
}

union UpdateBookTitleError = BookTitleTakenError | InvalidBookTitleError

interface Error {
  message: String!
}

type BookTitleTakenError implements Error {
  message: String!
  title: String!
}
```

### Error Mapping Strategies

**Direct exception** — exception `Message` becomes error message:
```csharp
[Error(typeof(BookTitleTakenException))]
```

**Factory method** — custom mapping:
```csharp
public class BookTitleTakenError
{
    public string Message { get; }
    public string Title { get; }

    public static BookTitleTakenError CreateErrorFrom(BookTitleTakenException ex)
        => new() { Message = $"'{ex.Title}' is taken.", Title = ex.Title };
}
```

**Constructor-based**:
```csharp
public class BookTitleTakenError
{
    public BookTitleTakenError(BookTitleTakenException ex)
    {
        Message = $"'{ex.Title}' is already taken.";
        Title = ex.Title;
    }
    public string Message { get; }
    public string Title { get; }
}
```

**DI factory** (for localization, logging, etc.):
```csharp
public class BookTitleTakenErrorFactory
    : IPayloadErrorFactory<BookTitleTakenException, BookTitleTakenError>
{
    public BookTitleTakenError CreateErrorFrom(BookTitleTakenException ex) => ...;
}
```

**Multiple errors** — throw `AggregateException`:
```csharp
if (errors.Count > 0)
    throw new AggregateException(errors);
```

### Custom Error Interface

```csharp
[GraphQLName("BookError")]
public interface IBookError
{
    string Message { get; }
    string Code { get; }
}

builder.Services.AddGraphQLServer()
    .AddMutationConventions(applyToAllMutations: true)
    .AddErrorInterfaceType<IBookError>();
```

### Convention Customization

```csharp
builder.Services.AddGraphQLServer()
    .AddMutationConventions(new MutationConventionOptions
    {
        InputArgumentName = "input",
        InputTypeNamePattern = "{MutationName}Input",
        PayloadTypeNamePattern = "{MutationName}Payload",
        PayloadErrorTypeNamePattern = "{MutationName}Error",
        PayloadErrorsFieldName = "errors",
        ApplyToAllMutations = true
    });

// Per-mutation override
[UseMutationConvention(InputTypeName = "RenameBookInput", PayloadTypeName = "RenameBookPayload")]

// Disable for specific mutation
[UseMutationConvention(Disable = true)]
```

---

## Relay Connections / Pagination

### Basic Usage

```csharp
[UsePaging]
public static IQueryable<Book> GetBooks(AppDbContext db)
    => db.Books.OrderBy(b => b.Id);
```

### Configuration

```csharp
// Per-field
[UsePaging(MaxPageSize = 100, DefaultPageSize = 25, IncludeTotalCount = true)]

// Global
builder.Services.AddGraphQLServer()
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = 100;
        opt.DefaultPageSize = 25;
        opt.IncludeTotalCount = true;        // Adds totalCount field
        opt.AllowBackwardPagination = true;  // Enables before/last
        opt.RequirePagingBoundaries = false;  // Mandates first or last
    });
```

### Custom Connection Name (Unique Per Context)

```csharp
// Generates AuthorBooksConnection + AuthorBooksEdge
[UsePaging(ConnectionName = "AuthorBooks")]
public static IQueryable<Book> GetBooks([Parent] Author author, AppDbContext db)
    => db.Books.Where(b => b.AuthorId == author.Id);
```

### Manual Connection Return

```csharp
[UsePaging]
public static async Task<Connection<Book>> GetBooksAsync(
    string? after, int? first,
    BookService service, CancellationToken ct)
{
    var result = await service.GetPageAsync(after, first, ct);
    var edges = result.Items
        .Select(b => new Edge<Book>(b, b.Id.ToString()))
        .ToList();

    return new Connection<Book>(
        edges,
        new ConnectionPageInfo(result.HasNext, result.HasPrev,
            edges.FirstOrDefault()?.Cursor, edges.LastOrDefault()?.Cursor),
        totalCount: _ => ValueTask.FromResult(result.Total));
}
```

### Extending Connection/Edge Types (Relationship Metadata)

```csharp
// Add computed field to connection
[ExtendObjectType("AuthorBooksConnection")]
public class AuthorBooksConnectionExtension
{
    public int GetPublishedCount([Parent] Connection<Book> connection)
        => connection.Edges.Count(e => e.Node.IsPublished);
}

// Add relationship data to edge
[ExtendObjectType("AuthorBooksEdge")]
public class AuthorBooksEdgeExtension
{
    public AuthorContribution GetContribution([Parent] Edge<Book> edge, AuthorService authors)
        => authors.GetContribution(edge.Node.Id);
}
```

### Page\<T\> Factory (v16 Breaking Change)

```csharp
// Page<T> is now abstract — use factory
return Page<Book>.Create(
    items,
    hasNextPage: hasNext,
    hasPreviousPage: false,
    createCursor: book => book.Id.ToString(),
    totalCount: totalCount);
```

### Middleware Order

When combining: `[UsePaging]` → `[UseProjection]` → `[UseFiltering]` → `[UseSorting]`

---

## DataLoaders (GreenDonut)

### Source-Generated Batch (One-to-One)

```csharp
[DataLoader]
public static async Task<Dictionary<Guid, Author>> GetAuthorByIdAsync(
    IReadOnlyList<Guid> ids,
    AppDbContext db,
    CancellationToken ct)
    => await db.Authors
        .Where(a => ids.Contains(a.Id))
        .ToDictionaryAsync(a => a.Id, ct);
```

Generated interface: `IAuthorByIdDataLoader`

### Source-Generated Group (One-to-Many)

```csharp
[DataLoader]
public static async Task<Dictionary<Guid, Book[]>> GetBooksByAuthorIdAsync(
    IReadOnlyList<Guid> authorIds,
    AppDbContext db,
    CancellationToken ct)
    => await db.Books
        .Where(b => authorIds.Contains(b.AuthorId))
        .GroupBy(b => b.AuthorId)
        .ToDictionaryAsync(g => g.Key, g => g.ToArray(), ct);
```

Array value (`TValue[]`) signals group semantics.

### Usage in Resolver

```csharp
public static async Task<Author?> GetAuthorAsync(
    [Parent] Book book,
    IAuthorByIdDataLoader loader,
    CancellationToken ct)
    => await loader.LoadAsync(book.AuthorId, ct);
```

### Key Rules

- Return `Dictionary<TKey, TValue>` (batch) or `Dictionary<TKey, TValue[]>` (group)
- `IReadOnlyList<TKey>` is a **rented list** — do not store or use outside the method
- Keys are deduplicated within a request
- Use `AsNoTracking()` for read-only loaders

---

## ID Handling / Relay

### Setup

```csharp
builder.Services.AddGraphQLServer()
    .AddGlobalObjectIdentification();
```

### Output Fields

```csharp
public class Book
{
    [ID] public Guid Id { get; set; }           // Relay global ID
    [ID<Author>] public Guid AuthorId { get; set; }  // Typed foreign key
}
```

### Input Parameters

```csharp
public static Book? GetBook(
    [ID] Guid id,                 // Any node ID
    [ID<Book>] Guid bookId,       // Only Book IDs accepted
    AppDbContext db) => ...;
```

### Node Interface

```csharp
[Node]
public class Book
{
    public Guid Id { get; set; }

    [NodeResolver]
    public static async Task<Book?> GetAsync(
        Guid id, AppDbContext db, CancellationToken ct)
        => await db.Books.FindAsync([id], ct);
}
```

---

## Authorization

**Important:** Use `HotChocolate.Authorization.AuthorizeAttribute`, NOT the Microsoft one.

```csharp
builder.Services.AddAuthorization();
builder.Services.AddGraphQLServer().AddAuthorization();

// Type-level (preferred — prevents hidden-path vulnerability)
[Authorize(Policy = "ReadBooks")]
public class Book { ... }

// Field-level (when needed)
[Authorize(Roles = ["Administrator"])]
public string InternalNotes { get; set; }

// Allow specific mutations without auth
[AllowAnonymous]
public static async Task<Author> RegisterAuthorAsync(...) { }
```

---

## v16 Key Breaking Changes

| Change | Action |
|--------|--------|
| `Page<T>` is abstract | Use `Page<T>.Create()` factory, zero-based cursor indices |
| Services auto-detected | Remove `[Service]` attributes from resolvers |
| `AddApplicationService<T>()` required | For services in schema pipeline (error filters, interceptors) |
| Batching disabled by default | Enable explicitly: `o.Batching = AllowedBatching.All` |
| `Any` and `Json` merged | Use `[GraphQLType<AnyType>]`, add `.AddJsonTypeConverter()` |
| `URL` → `URI`, `Byte` → `UnsignedByte` | Update type references |
| Eager initialization at startup | Remove `InitializeOnStartup()` calls |
| `IRequestContext` removed | Use `context.OperationDocumentInfo` instead |
| DateTime serialization | Up to 7 fractional seconds (was 3) |
