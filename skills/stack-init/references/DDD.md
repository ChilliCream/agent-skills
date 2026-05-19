# Domain-Driven Design reference

DDD is a set of modelling techniques — bounded contexts, aggregates, value objects, domain events, repositories, ubiquitous language. It composes with Clean Architecture (which dictates *where* code lives); DDD dictates *what shape* the domain model takes.

Use this reference when the codebase has multiple distinct subdomains (e.g., `Catalog/`, `Identity/`, `Billing/`), each with their own entities and language, or when the user explicitly asks for DDD.

## Philosophy

Pick DDD when language and invariants are the hard part. It is an overlay on the code structure: combine it with Clean Architecture, Hexagonal, or GraphQL-first so agents know both what the domain means and where implementation code belongs.

## Bounded contexts

A bounded context is a boundary inside which one model is consistent. Across the boundary the same word can mean different things (a `Customer` in Sales is not a `Customer` in Support).

In a .NET solution, a bounded context typically maps to:

- A top-level folder per context (`Billing/`, `Identity/`, `Catalog/`), each containing its own `Domain/`, `Application/`, `Infrastructure/` subfolders. Or
- A separate solution per context, if they are deployed independently.

Document each context with: name, purpose, the upstream/downstream contexts it talks to, and the integration style (shared kernel, customer-supplier, anti-corruption layer).

## Aggregates

An aggregate is a cluster of entities and value objects treated as one unit for consistency. Each aggregate has a single **aggregate root** — the only entity outside code can reference directly. Mutations to the aggregate go through the root.

Rules:

- One transaction per aggregate. Saving an aggregate persists every entity inside it atomically.
- References across aggregates use ids, not object references. `Order.CustomerId` is a `Guid`, not a `Customer`.
- Aggregates protect invariants. If "an Order's total must equal the sum of its line items" is a rule, `OrderLine` lives inside the `Order` aggregate and is created via `Order.AddLine(...)`.

Keep aggregates small. An `Author` that owns thousands of `Book`s with their full revision history is a smell — split it (e.g., move revision history to its own aggregate referenced by id).

## Entities

Objects with identity. Two entities with the same property values are still different entities if their ids differ. A typical entity convention:

```csharp
public sealed class Book : Entity
{
    internal Book(
        Guid id,
        Guid authorId,
        DateTimeOffset createdAt,
        UserInfo createdBy,
        string title)
    {
        Id = id;
        AuthorId = authorId;
        Title = title;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public Guid Id { get; internal set; }
    public Guid AuthorId { get; internal set; }
    public Author? Author { get; internal set; }
    public string Title { get; internal set; }
    public DateTimeOffset CreatedAt { get; internal set; }
    public UserInfo CreatedBy { get; internal set; }

    private readonly List<BookEdition> _editions = new();
    public IReadOnlyList<BookEdition> Editions => _editions;

    public static Book Create(Guid authorId, UserInfo createdBy, string title)
    {
        return new Book(Guid.CreateVersion7(), authorId, DateTimeOffset.UtcNow, createdBy, title);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        Title = title;
        Events.Add(new BookRenamedEvent(this));
    }

    protected override void OnRemove()
    {
        Events.Add(new BookRemovedEvent(this));
    }
}
```

Notice: `internal` constructor, `internal set` properties, factory `Create` method, behaviour as instance methods (`Rename`), event collection raised from inside the methods. No public setters — every state change goes through a named method that enforces invariants.

In this model, `Author` is the aggregate root and `Book` is an entity inside the `Author` aggregate — Books have their own identity, but loading/saving them goes through the Author. `BookEdition` is a child entity nested another level deeper.

## Value objects

Immutable, no identity, equal when their fields are equal. Use `record` in C#:

```csharp
public sealed record UserInfo(Guid Id, string Email, string DisplayName);
public sealed record Isbn(string Value);
```

Or with validation in the constructor:

```csharp
public sealed record Money
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter code.");

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; init; }
    public string Currency { get; init; }
}
```

Prefer value objects over primitives — `Money` is more honest than `(decimal Amount, string Currency)` tuples scattered through the code.

## Domain events

Records that describe something that has happened in the past tense (`BookRenamed`, never `RenameBook`). Stored on the entity, dispatched after the aggregate is saved.

```csharp
public sealed record BookRenamedEvent(Book Book) : IDomainEvent;
```

Raised from inside the entity:

```csharp
public void Rename(string title)
{
    Title = title;
    Events.Add(new BookRenamedEvent(this));
}
```

Handled by separate handler classes in the Application layer. The entity does not know who listens — it announces; subscribers react.

## Repositories

Abstractions that look like in-memory collections of aggregates. `IAuthorRepository.GetById(Guid)`, `Add(Author)`, `Remove(Author)`.

In EF-based stacks, repositories are often skipped in favor of:

- `IAppContext.Authors` (`DbSet<Author>`) for writes.
- DataLoaders for reads.

This is a deliberate choice. Repositories add a layer that EF Core already provides. Use them only if you must hide a non-EF data source (a Cosmos client, a remote service) behind a domain-shaped interface.

## Ubiquitous language

Names in code match names domain experts use. If accountants say "invoice", call the class `Invoice`, not `BillingDocument`. Keep a glossary in `DOMAIN.md` so new contributors learn the vocabulary.

When the same word means different things in different contexts, document the distinction explicitly: *"In Billing, an `Account` is a customer's billing relationship. In Identity, an `Account` is a sign-in identity."* If the confusion is bad enough, rename one of them.

## Anti-corruption layer

When a bounded context talks to a legacy or external system whose model would pollute yours, introduce an ACL — a translation layer that maps the external model to your domain. Place the ACL on your side of the boundary so changes upstream do not ripple into your aggregates.

## Common mistakes

- **Anemic domain model.** Entities with public setters and no behaviour, business logic spread across service classes. The domain becomes a data bag and Application becomes a procedural script.
- **Aggregates that are too large.** "The `Author` owns every book, edition, and revision." Now every save is a giant transaction, every load pulls in megabytes. Split.
- **Cross-aggregate object references.** `Order.Customer` instead of `Order.CustomerId`. Now you cannot save `Order` without considering `Customer`, and the aggregate boundary is fiction.
- **Synchronous event handling.** Domain events that block the originating transaction defeat the point. Dispatch after commit, idempotently.
- **No ubiquitous language.** The code says `Item`, the spec says `Product`, the database column is `Sku`. Pick one and propagate it.

## Agentic coding preparation

`DOMAIN.md` is the critical artifact for DDD. It should include:

- Bounded contexts and integration relationships.
- Aggregate roots and the child entities they own.
- Cross-aggregate references by id.
- Value objects and validation rules.
- Domain events and when they are raised.
- Ubiquitous language, including terms that differ between contexts.

Agents must update `DOMAIN.md` whenever they add a domain concept, invariant, event, or context boundary.

## Reference project

Use `assets/reference-projects/domain-driven-design/` as the starter shape. It demonstrates multiple bounded contexts with separate domain models.
