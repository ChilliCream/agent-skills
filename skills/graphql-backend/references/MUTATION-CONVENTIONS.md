# MUTATION-CONVENTIONS — auto-wrapping inputs and payloads

HotChocolate's *mutation conventions* take a flat method signature and generate a Relay-shaped GraphQL mutation around it. This is on by default in a HotChocolate GraphQL backend. **Hand-rolling input or payload types defeats the conventions and is almost always wrong.**

## What the convention generates

Given this method:

```csharp
[MutationType]
public class BookMutations
{
    [Authorize]
    [Error<DuplicateTitleError>]
    [Error<UnauthorizedOperation>]
    public async ValueTask<Book> CreateBookAsync(
        [ID<Author>] Guid authorId,
        string title,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
        => await sender.SendAsync(new CreateBookCommand(user, authorId, title), ct);
}
```

The convention emits the following GraphQL:

```graphql
input CreateBookInput {
  authorId: ID!
  title: String!
}

union CreateBookError = DuplicateTitleError | UnauthorizedOperation

type CreateBookPayload {
  book: Book
  errors: [CreateBookError!]
}

type Mutation {
  createBook(input: CreateBookInput!): CreateBookPayload!
}
```

Three things were generated:

1. **Input type** — `CreateBookInput` wraps every non-service parameter from the method.
2. **Payload type** — `CreateBookPayload` wraps the return (`Book` becomes a nullable `book` field) and an `errors` array.
3. **Error union** — `CreateBookError` is built from every `[Error<T>]` declared on the method.

The middleware:

- Takes the `input` argument from the client and unpacks it back to method parameters.
- Catches exceptions of declared error types and maps them into `errors`. On success, `book` is populated and `errors` is empty/null.
- Excludes services (`ISender`, `ClaimsPrincipal`, `IFoo`), `[Parent]`, `CancellationToken`, and `IResolverContext` from the input.

## Why hand-rolling is wrong

**Hand-rolled input:**

```csharp
public record CreateBookInput(
    [property: ID<Author>] Guid AuthorId,
    string Title);

public async ValueTask<Book> CreateBook(CreateBookInput input, ...)
```

What breaks:

- The schema still emits `CreateBookInput`, but now the convention *also* wraps your `CreateBookInput` parameter into an outer `CreateBookInput`. Either the build collides on the type name, or you end up with a doubly-nested input.
- The introspected schema looks slightly off — `mutation { createBook(input: { input: { authorId: ..., title: ... } }) }`. Clients break.

**Hand-rolled payload:**

```csharp
public record CreateBookPayload(Book Book);

public async Task<CreateBookPayload> CreateBook(...)
{
    var book = await sender.SendAsync(...);
    return new CreateBookPayload(book);
}
```

What breaks:

- The convention sees the return type and tries to wrap it. Now `CreateBookPayload` contains a `createBookPayload` field of type `CreateBookPayload` (depending on naming).
- More commonly: the convention's error-mapping middleware looks for the return type to extract the entity. Your hand-rolled type defeats this, so `[Error<T>]` no longer populates the `errors` field. Exceptions surface as top-level GraphQL errors instead.

**Hand-rolled error union:**

There is no equivalent — `[Error<T>]` *is* how you declare the union. Adding an `Errors` field to a hand-rolled payload type does nothing; the middleware doesn't recognize it.

## When hand-rolling an input is correct

Two narrow edge cases — both rare. If neither applies, let the convention generate the input.

### 1. Shared input across multiple mutations

If multiple mutations all accept the same complex input shape, define it once and pass it as a parameter:

```csharp
// In <Entity>/Inputs/PriceRangeInput.cs
public sealed record PriceRangeInput(decimal Min, decimal Max)
{
    public PriceRange ToPriceRange() => new(Min, Max);
}

// In a mutation method:
public async ValueTask<Book> UpdateBookPriceRangeAsync(
    [ID<Book>] Guid bookId,
    PriceRangeInput priceRange,            // shared input as a parameter
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken ct)
    => await sender.SendAsync(new UpdateBookPriceRangeCommand(user, bookId, priceRange.ToPriceRange()), ct);
```

The convention still wraps the whole signature into `UpdateBookPriceRangeInput { bookId, priceRange }`. `PriceRangeInput` is a nested input type, shared across mutations.

This is the only path to "reuse the same input". If you find yourself defining `CreateBookInput` and `CreateBookInput2`, you're doing it wrong — let the convention generate per-mutation inputs.

### 2. `@oneOf` inputs

GraphQL's `@oneOf` (exactly one of N fields must be set) cannot be expressed by the convention because the convention treats parameters as a fixed shape. For these, hand-roll an input type:

```csharp
/// <summary>
/// @oneOf
/// </summary>
public record BookChangeInput(
    BookCreateChange? Create,
    BookUpdateChange? Update,
    BookDeleteChange? Delete)
    : IHasOneOf
{
    public IBookChange OneOf()
        => OneOfHelper.EnsureOneOf<IBookChange>(Create, Update, Delete);
}
```

Hand-roll the input, accept it as a method parameter, and the convention will still wrap it in an outer per-mutation input. The `@oneOf` constraint applies to the inner type.

## `[UseMutationConvention(...)]` overrides

The convention's defaults can be tweaked on a per-method basis:

```csharp
[UseMutationConvention(PayloadFieldName = "id")]
[ID("BookPublishRequest")]
public async ValueTask<Guid> PublishBookAsync(...)
    => await sender.SendAsync(command, ct);
```

Generated schema:

```graphql
type PublishBookPayload {
  id: BookPublishRequest    # named via PayloadFieldName
  errors: [PublishBookError!]
}
```

Without the override, the field would be `guid: UUID` — meaningless to the client. With `PayloadFieldName = "id"` plus `[ID("BookPublishRequest")]`, the field becomes a Relay ID typed as `BookPublishRequest`.

Common overrides:

- `PayloadFieldName = "<name>"` — when the entity name doesn't make a clean field name (e.g. returning a primitive, a `Guid`, or a result interface).
- `Disable = true` — fully disable conventions for a method. **Avoid in normal code.** It only exists for edge cases like proxy mutations.

## Inputs folder

`<Entity>/Inputs/` exists only for:

- `@oneOf` inputs.
- Genuinely shared inputs reused across multiple mutations.
- Refactored change/event payloads.

It should **not** contain per-mutation inputs. If you see `CreateBookInput.cs` next to `CreateBook` in `BookMutations.cs`, delete the file and let the convention generate it.

## Quick checks

When reviewing a mutation, run through this list:

- [ ] Method returns `ValueTask<TEntity>`, not `ValueTask<TPayload>`.
- [ ] Inputs are flat method parameters; complex types come from `<Entity>/Inputs/` only if shared or `@oneOf`.
- [ ] `[Error<T>]` declares every domain error thrown by the handler.
- [ ] `ISender`, `ClaimsPrincipal`, `CancellationToken` are injected — services are not wrapped into the input.
- [ ] No try/catch swallowing exceptions in the resolver.
- [ ] No `<Mutation>Input` or `<Mutation>Payload` type defined in the codebase that the convention would otherwise generate.

If all six pass, the mutation conventions will produce the right schema and the right error mapping.
