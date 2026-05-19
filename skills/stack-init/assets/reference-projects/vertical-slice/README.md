# Reference.VerticalSlice

A compact HotChocolate reference service that keeps behavior in vertical slices. Each slice owns its Mocha command/query message, handler, GraphQL wrapper, and tests. Shared code is limited to domain entities, EF Core persistence, policy names, and DataLoaders that batch cross-slice reads.

The sample uses Authors and Books. It intentionally avoids repositories, service layers, mapping profiles, and DTO stacks. Add a new feature by copying the closest slice and keeping the resolver thin: GraphQL translates arguments, Mocha dispatches, handlers use EF Core or DataLoaders directly.

Simple invariants live on the domain model. Add a feature validator only when the request has cross-field rules or external checks that do not belong on the entity.
