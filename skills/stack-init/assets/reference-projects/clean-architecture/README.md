# Clean Architecture GraphQL reference

This asset is a realistic starter shape for a small HotChocolate service that uses Clean Architecture without ceremonial layers.

## Shape

```text
src/
  Reference.CleanArchitecture.Domain/          # aggregates, value objects, domain events
  Reference.CleanArchitecture.Application/     # Mocha commands, queries, DataLoaders, policies
  Reference.CleanArchitecture.Infrastructure/  # EF Core DbContext and configurations
  Reference.CleanArchitecture.GraphQL/         # thin HotChocolate query/mutation/type wrappers
  Reference.CleanArchitecture.Host/            # composition root
test/
  Reference.CleanArchitecture.Domain.Tests/
  Reference.CleanArchitecture.Application.Tests/
  Reference.CleanArchitecture.GraphQL.Tests/
```

## Architectural rules

- Domain owns invariants. Entities expose behavior, not public setters.
- Application owns use cases. Commands mutate through EF Core; queries read through DataLoaders.
- Infrastructure owns provider details. The reference uses SQLite by default because it is easy to run locally.
- GraphQL is a transport adapter. Resolvers translate arguments and dispatch Mocha messages through `ISender`.
- No repository, DTO, mapper, or service layer exists unless the abstraction carries real value.

## Demonstrated flow

- `RegisterAuthorCommand` creates an `Author` aggregate.
- `AddBookToAuthorCommand` loads an author, authorizes access, calls `Author.AddBook`, persists with EF Core, and publishes loaded entities to the GreenDonut promise cache.
- `GetBookByIdQuery` and `GetAuthorByIdQuery` read through generated batching contexts.
- `AuthorType` and `BookType` expose Relay node resolvers and navigation fields without touching EF Core.

## Package assumptions

The project intentionally uses current ChilliCream-style package names (`HotChocolate`, `GreenDonut`, `Mocha.Mediator`). If the consuming repository pins versions centrally, copy the project files and replace the sample versions in `Directory.Packages.props` with the repository's versions.
