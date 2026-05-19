# Domain-driven GraphQL reference project

This reference shows Domain-Driven Design as an overlay on a normal layered .NET application:

- `Reference.Ddd.Catalog` owns the Catalog bounded context and the `Product` aggregate.
- `Reference.Ddd.Ordering` owns the Ordering bounded context and the `Order` aggregate.
- `Reference.Ddd.SharedKernel` contains only concepts both contexts truly share: domain events, aggregate event storage, and `Money`.
- `Reference.Ddd.Application` contains Mocha mediator commands/queries and GreenDonut DataLoaders per bounded context.
- `Reference.Ddd.Infrastructure` contains EF Core DbContexts and mappings. The sample uses one database connection but keeps separate `CatalogDbContext` and `OrderingDbContext` boundaries.
- `Reference.Ddd.GraphQL` exposes thin HotChocolate resolvers. Resolvers dispatch Mocha commands/queries and do not contain business logic.
- `Reference.Ddd.Host` wires EF Core, HotChocolate, GreenDonut DataLoaders, authorization, and Mocha.

The model deliberately avoids repositories, DTO mapping layers, and generic services. EF Core is the unit of work for commands, DataLoaders are the read batching boundary, and aggregates enforce invariants.

## Boundary rules

- Mutate an aggregate only through methods on its root (`Product.ChangePrice`, `Order.AddLine`, `Order.Submit`).
- Reference another aggregate or bounded context by id. `OrderLine.ProductId` is a Catalog product id; it is not a `Product` navigation.
- Copy facts needed by the receiving context. Ordering stores product SKU/name/price snapshots because an order must not change when Catalog renames a product.
- Query handlers load through generated batching contexts. Command handlers load through EF Core DbContexts so they save a tracked aggregate in one unit of work.
- GraphQL node/type resolvers dispatch through `ISender.QueryAsync`; mutations dispatch through `ISender.SendAsync`.

## Generator assumptions

The host shows the expected source-generated registration points:

- `services.AddMediator().AddApplication()` from Mocha mediator.
- `requestExecutorBuilder.AddApplicationTypes()` from HotChocolate/GreenDonut type and DataLoader generation.

If your package versions generate different extension names, keep the same architecture and adjust only the generated registration calls.
