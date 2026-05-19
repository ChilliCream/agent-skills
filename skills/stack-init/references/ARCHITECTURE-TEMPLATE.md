<!--
ARCHITECTURE.md template. Copy this to the target repo's root and replace every
{placeholder}. Delete sections that do not apply. When uncertain, add a TODO
marker with a reason and report it back to the user during the verify step.
-->

# Architecture

## Pattern

{pattern-name} (e.g., Clean Architecture, Vertical Slice, GraphQL-first, Hexagonal, DDD overlay).

{one-paragraph-rationale: why this pattern fits the system. Reference the project layout that proves it — "the `src/` folder has four projects following the Domain/Application/Infrastructure/Presentation convention".}

## Setup decision log

| Question | Decision | Evidence | Rationale |
| -------- | -------- | -------- | --------- |
| Architecture style | {chosen style} | {repo paths or "new repo"} | {why this style was recommended/confirmed} |
| Delivery surface | {GraphQL / REST / worker / multiple} | {files/packages} | {why this surface leads or follows} |
| Domain depth | {CRUD / rich aggregates / bounded contexts} | {entities/events/TODO} | {how much domain modeling to use} |
| Persistence | {EF Core / Dapper / none / TODO} | {DbContext/migrations/packages} | {how persistence is isolated} |
| Bootstrap scope | {docs only / docs + scaffold / retrofit} | {user confirmation} | {what was intentionally created or left alone} |

## Structure and responsibilities

### {Boundary 1 - e.g., Domain / Core / Feature / Schema}

**Project:** `{actual.csproj.name}`

{one-paragraph-purpose}

Contains:
- {category, e.g., entities and value objects}
- {category, e.g., domain events}
- {category, e.g., invariants enforced in entity methods}

Forbidden:
- {what does not belong here, e.g., framework references, DbContext, HTTP types}

### {Boundary 2 - e.g., Application / Use Cases / Shared Infrastructure}

**Project:** `{actual.csproj.name}`

{purpose}

Contains:
- {commands and handlers}
- {queries and DataLoaders}
- {the IAppContext (or equivalent) DbContext abstraction}

Depends on: {previous-layer}.

### {Boundary 3 - e.g., Infrastructure / Driven adapters / Persistence}

**Project:** `{actual.csproj.name}`

{purpose}

Contains:
- {EF Core context implementation}
- {entity configurations and migrations}
- {external service clients}

Depends on: {previous-layer}.

### {Boundary 4 - e.g., Presentation / GraphQL / Driving adapters}

**Project:** `{actual.csproj.name}`

{purpose}

Contains:
- {ObjectType / controller / endpoint definitions}

Depends on: {layer it talks to}.

## Dependency rules

```
{Layer4} ──► {Layer2} ──► {Layer1} ◄── {Layer3}
```

- {Layer1} depends on nothing.
- {Layer2} depends only on {Layer1}.
- {Layer3} depends on {Layer2} and implements abstractions declared there.
- {Layer4} depends on {Layer2} only — never references {Layer3} directly.
- Composition root (the `{Host.csproj}`) wires implementations to abstractions via DI.

## Folder layout

```
{paste real `tree`-style output of the repo. Do not invent paths.}
```

## Naming conventions

- {Entities live in `{Layer1}/Entities/{Plural}/`, one folder per entity.}
- {Domain events live in `{Layer1}/Entities/{Plural}/Events/`.}
- {Commands are named `{Verb}{Noun}Command` and colocated with handlers in one file.}
- {Queries are named `Get{Noun}By{Criterion}` or `Page{Plural}By{Criterion}` and contain an `ExecuteAsync` method.}
- {GraphQL types are `{Noun}Type` and decorated with `[ObjectType<Noun>]`.}
- {File-scoped namespaces, 4-space indent, curly braces always.}

## Cross-cutting concerns

- **Logging.** {Where logging is configured — typically the Host. Structured logs via Serilog/OpenTelemetry.}
- **Validation.** {Where input validation lives — Application pipeline behaviors vs entity-method invariants.}
- **Authentication / authorization.** {Where session lookup and policy checks happen.}
- **Transactions.** {How `SaveChangesAsync` is called — once per command at the end of the handler.}
- **Domain events.** {How events are dispatched — after the transaction commits, via outbox / mediator notifications.}
- **Integration events.** {Outbound integration events: where they are published from and to which transport.}

## Agentic coding contract

Agents making changes in this repo must:

1. Read this file and [`DOMAIN.md`](./DOMAIN.md) before planning implementation work.
2. Cite the layer/slice/port/schema rule that justifies every new file.
3. Follow existing naming and folder conventions before inventing a new one.
4. Keep business rules in the domain/application/core location named above, not in transport or infrastructure code.
5. Update this file when a new architectural rule, project, adapter, or cross-cutting mechanism is introduced.
6. Update `DOMAIN.md` when a domain concept, invariant, aggregate boundary, value object, or domain event changes.

Do not:

- Add a new top-level folder, project, adapter, or dependency direction without updating this document.
- Duplicate domain language under a different name.
- Leave generated TODOs unresolved without explaining why.

## How to add a new feature

1. **{Step 1 — Domain.}** {What to add to the domain layer: entity, value object, events, invariants.}
2. **{Step 2 — Application.}** {What command(s) and handler(s) to add; what queries and DataLoaders.}
3. **{Step 3 — Infrastructure.}** {What EF configuration and migration to add.}
4. **{Step 4 — Presentation.}** {What GraphQL type / REST controller / CLI command to add.}
5. **{Step 5 — Tests.}** {Where to put unit tests for the domain and integration tests for the use case.}

Each step touches one architectural boundary at a time. If you find yourself mixing domain behavior, transport wiring, and infrastructure in one step, the boundaries are wrong.

## See also

- [`DOMAIN.md`](./DOMAIN.md) — entities, value objects, domain events, invariants.
