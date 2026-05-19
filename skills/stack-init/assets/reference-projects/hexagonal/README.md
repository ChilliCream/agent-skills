# Hexagonal GraphQL reference project

This reference shows a HotChocolate GraphQL service around a ports-and-adapters core.

Rules demonstrated here:

- `Reference.Hexagonal.Core` owns domain entities, use cases, and ports. It has no EF Core, HotChocolate, GreenDonut, Mocha, ASP.NET, or GraphQL references.
- Mocha mediator messages live in the GraphQL driving adapter. They adapt GraphQL/ASP.NET concerns such as `ClaimsPrincipal`, dispatch through `ISender`, and call core input ports.
- Persistence implements core output ports with EF Core. Read ports are backed by GreenDonut DataLoaders so GraphQL reads batch and cache without the core knowing GreenDonut exists.
- GraphQL resolvers are thin: translate IDs/arguments, dispatch commands or queries, and return the domain object.
- There are no generic repositories, DTO mapping layers, or service abstractions that do not protect an architectural boundary.

The sample domain is a small library catalog with `Author` and `Book`. Reads are public. Writes require an authenticated user at the GraphQL/Mocha boundary; the core receives domain-shaped inputs only.

## Projects

```text
src/
  Reference.Hexagonal.Core/
  Reference.Hexagonal.Adapters.Persistence/
  Reference.Hexagonal.Adapters.GraphQL/
  Reference.Hexagonal.Host/
test/
  Reference.Hexagonal.Core.Tests/
  Reference.Hexagonal.Adapters.Persistence.Tests/
  Reference.Hexagonal.Adapters.GraphQL.Tests/
```

## Adding a feature

1. Add domain behavior and input/output ports in Core.
2. Implement output ports in Persistence when the core needs data or durability.
3. Add Mocha command/query messages in the GraphQL adapter to adapt GraphQL callers to core ports.
4. Add `[QueryType]`, `[MutationType]`, or `[ObjectType<T>]` methods that dispatch via `ISender`.
5. Wire the adapter in Host only.

If an abstraction does not keep the core free from a concrete adapter, do not add it.
