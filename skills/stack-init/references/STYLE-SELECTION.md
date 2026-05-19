# Style selection reference

Use this reference to turn repository evidence into a recommendation. The setup conversation should feel like an architectural interview, not a blank survey: every question starts with what the repo already says.

## Recommendation matrix

| Style | Recommend when you observe | Avoid when |
|---|---|---|
| Clean Architecture | `*.Domain`, `*.Application`, `*.Infrastructure`, `*.GraphQL` / `*.Web`, command/query handlers, EF configurations, domain events, multiple delivery surfaces. | The app is a small CRUD/API service and horizontal layers would add ceremony without protecting a rich domain. |
| Vertical Slice | `Features/**`, minimal APIs, colocated requests/handlers/validators/endpoints, one deployable app, frequent independent features. | Invariants span many entities or multiple transports need the same use cases. |
| GraphQL-first | `.graphql` SDL files, schema snapshots, HotChocolate types/resolvers, client-driven API design, Relay nodes, Fusion subgraphs, schema reviews before implementation. | GraphQL is only a thin internal adapter and clients do not treat the schema as the product contract. |
| Hexagonal | `Core/`, `Ports/`, `Adapters/`, many inbound/outbound adapters, strong need to test the core with fake ports. | The team wants a prescriptive .NET folder recipe more than port vocabulary. |
| DDD overlay | Bounded-context folders, aggregate roots, value objects, domain events, ubiquitous language, context maps, distinct subdomains. | The domain is mostly CRUD and does not justify aggregate modeling. |
| Document existing shape | Flat projects, legacy folders, mixed patterns, or a repo where moving files is risky. | A new repo where the user explicitly wants a clean scaffold. |

## Questionnaire pattern

Ask one question at a time in this structure:

```markdown
Observed: `<path>` and `<path>` show <fact>.
Recommendation: <option>.
Why: <trade-off>.
Question: <focused multiple-choice question>
```

Good:

> Observed: `src/Catalog.Domain`, `src/Catalog.Application`, and `src/Catalog.Infrastructure` already exist.
> Recommendation: keep Clean Architecture and add a DDD overlay for aggregate documentation.
> Why: moving to Vertical Slice would force broad file moves, while the current projects already encode dependency direction.
> Question: Should I document and scaffold around Clean Architecture?

Bad:

> Which architecture do you want?

The bad version ignores the codebase and makes the user repeat information the repo already contains.

## Question decision tree

1. **Architecture style.**
   - Strong existing signal: recommend preserving it.
   - Empty repo with GraphQL product goal: recommend GraphQL-first over Clean Architecture.
   - Empty repo with rich business rules: recommend Clean Architecture with DDD overlay.
   - Empty repo with simple feature CRUD: recommend Vertical Slice.
   - Many adapters or plugin-like boundaries: recommend Hexagonal.

2. **Delivery surface.**
   - HotChocolate attributes or `.graphql` files: recommend GraphQL.
   - Controllers/minimal API endpoints only: recommend REST/minimal API.
   - Consumers/workers/message handlers: recommend message bus/worker.
   - Multiple adapters: recommend Hexagonal or Clean Architecture with separate presentation projects.

3. **Domain depth.**
   - Entities with methods, value objects, events, invariants: recommend rich domain / DDD overlay.
   - DTOs with public setters and services doing logic: recommend documenting current anemic model or refactoring toward richer aggregates only if the user wants that migration.
   - No model yet: ask whether the first features are CRUD or invariant-heavy.

4. **Persistence.**
   - EF packages, migrations, `DbContext`, `IEntityTypeConfiguration<T>`: recommend EF Core.
   - SQL clients/repositories without EF: recommend Dapper/raw SQL.
   - No persistence: ask whether to scaffold an interface/port only.

5. **Bootstrap scope.**
   - Existing production repo: recommend docs plus minimal missing folders.
   - Empty repo: recommend docs plus project skeleton.
   - Legacy mixed repo: recommend docs first, then a migration plan.

## Recommendation wording

Use direct recommendations. Do not hide behind "it depends" unless the evidence is genuinely balanced.

Preferred:

> Recommendation: Clean Architecture with DDD modeling. The existing `.Domain` / `.Application` / `.Infrastructure` projects already enforce inward dependencies, and `*Event.cs` files show a domain-event model.

Avoid:

> You could use Clean Architecture or Vertical Slice.

If two options are close, rank them and explain the deciding question:

> Clean Architecture is my first choice if this service will grow a rich domain. Vertical Slice is better if most work is independent CRUD screens. Which future is closer?
