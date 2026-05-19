# GraphQL-first reference project

This reference shows a schema/API-first HotChocolate service where the GraphQL contract leads the implementation and resolvers stay thin.

## Shape

```text
src/
  Reference.GraphQLFirst.Domain/          # Author and Book domain entities/invariants
  Reference.GraphQLFirst.Application/     # Mocha commands, queries, DataLoaders
  Reference.GraphQLFirst.Infrastructure/  # EF Core DbContext and configuration
  Reference.GraphQLFirst.GraphQL/         # HotChocolate types, queries, mutations, SDL proposal
  Reference.GraphQLFirst.Host/            # DI, GraphQL server, EF Core, mediator wiring
test/
  Reference.GraphQLFirst.Domain.Tests/
  Reference.GraphQLFirst.Application.Tests/
  Reference.GraphQLFirst.GraphQL.Tests/
```

## Rules demonstrated

- Start with `src/Reference.GraphQLFirst.GraphQL/Schema/library.graphql` as the reviewed API contract.
- Keep business behavior in Domain/Application. GraphQL resolvers only translate arguments and call `ISender.SendAsync` or `ISender.QueryAsync`.
- Use Mocha mediator primitives exclusively.
- Use GreenDonut/HotChocolate DataLoaders for reads and navigation fields.
- Use EF Core directly through `IReferenceDbContext`; no fake repositories or mapping layers.
- Distinguish GraphQL transport from domain: `Book.AuthorId` is a domain/persistence fact, while GraphQL exposes `Book.author` through `BookType`.
- Schema snapshots live in `test/Reference.GraphQLFirst.GraphQL.Tests/__snapshots__/schema.snap`.

## Version assumptions

The sample targets the current ChilliCream conventions: HotChocolate source-generator attributes, GreenDonut `[DataLoader]`/`[DataLoaderGroup]`, and Mocha mediator source-generated registration (`AddMediator().AddApplication()`). If a consuming repo pins different package versions, adjust the generated extension names and paged DataLoader call shape to that version.
