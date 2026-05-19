# GraphQL-first reference

GraphQL-first means the schema contract leads the implementation. Design the GraphQL API first, review it as a client contract, then implement thin HotChocolate resolvers over application commands, queries, and domain behavior.

This is not permission to put business logic in resolvers. The GraphQL layer owns the API contract; the Application and Domain layers own behavior.

## Philosophy

- **Schema is the product boundary.** Client workflows, nullability, pagination, mutations, and errors are decided before storage or resolver details.
- **Resolvers stay thin.** A resolver translates GraphQL arguments, dispatches a command/query, and returns the result.
- **Domain still protects invariants.** GraphQL input validation is not enough; aggregate factories and methods enforce rules that must hold from any transport.
- **Schema snapshots are tests.** A schema diff is reviewed like a public API change.

## Folder layout

```
src/
  MyApp.GraphQL/
    Schema/
      books.graphql                 # SDL when the project is schema-first
    Books/
      Operations/
        BookQueries.cs              # [QueryType] wrappers
        BookMutations.cs            # [MutationType] wrappers
      Types/
        BookType.cs                 # [ObjectType<Book>] and field resolvers
  MyApp.Application/
    Books/
      Commands/
        CreateBookCommand.cs
      Queries/
        GetBookByIdQuery.cs
      DataLoaders/
        BookDataLoaders.cs

  MyApp.Domain/
    Books/
      Book.cs
      Events/
        BookCreatedEvent.cs

  MyApp.Infrastructure/
    AppDbContext.cs
    EntityConfigurations/

test/
  MyApp.GraphQL.Tests/
    __snapshots__/
      schema.snap
```

If the repo uses HotChocolate code-first rather than SDL, keep the same intent: schema design is approved first, then `[ObjectType<T>]`, `[QueryType]`, and `[MutationType]` code implements it.

## When to pick GraphQL-first

- The GraphQL schema is consumed by multiple clients or teams.
- The team reviews schema proposals before implementation.
- The service exposes Relay nodes, connections, mutation payloads, error unions, or Fusion subgraphs.
- Client workflows are more stable than database shape.

Avoid it when GraphQL is only an internal adapter over an already-set application model. In that case, use Clean Architecture or Vertical Slice and keep GraphQL as the presentation layer.

## Setup questionnaire hints

Ask whether the user wants schema design to gate implementation:

> Observed: `.graphql` files and schema snapshots exist.
> Recommendation: GraphQL-first.
> Why: the repo already treats schema changes as reviewed API contracts, so architecture docs should make schema design the first step for every feature.
> Question: Should new features start with a schema proposal before application/domain code?

If no schema exists but the user says "HotChocolate service" or "GraphQL API", recommend GraphQL-first over Clean Architecture for new public APIs.

## How to add a feature

1. Design or update the SDL / schema proposal using the `graphql-schema-design` skill.
2. Add or update Domain types only for real business concepts, not for every GraphQL input.
3. Add Application commands and queries that model the approved operations.
4. Add GraphQL wrappers that dispatch through `ISender.SendAsync` or `ISender.QueryAsync`.
5. Add/update schema snapshots and resolver tests.
6. Update `ARCHITECTURE.md` if the folder or dependency rules changed; update `DOMAIN.md` if aggregates, events, or language changed.

## Agentic coding preparation

`ARCHITECTURE.md` should explicitly state:

- Schema proposals come before implementation for public API changes.
- Resolvers contain no business logic and never access EF Core directly.
- Mutations follow HotChocolate mutation conventions and expose typed errors.
- Lists require a pagination decision.
- Schema snapshots are reviewed before merge.

`DOMAIN.md` should explicitly state which GraphQL nouns are real domain concepts and which are transport-only DTOs. This prevents agents from creating domain entities just because a GraphQL type exists.

## Reference project

Use `assets/reference-projects/graphql-first/` as the starter shape. It shows a GraphQL project leading the feature folders while delegating behavior to Application and Domain projects.
