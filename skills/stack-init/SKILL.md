---
name: stack-init
description: Guide new or existing repositories through architecture setup with repo inspection, a recommendation-backed questionnaire, optional reference-project scaffolding, and agent-ready ARCHITECTURE.md / DOMAIN.md docs. Use when the user says "initialize stack", "setup skill", "set up architecture", "/stack-init", "new repo", "bootstrap architecture", "scaffold architecture", "Clean Architecture", "Vertical Slice", "GraphQL-first", "Hexagonal", "DDD", "missing ARCHITECTURE.md", "missing DOMAIN.md", or when starting a .NET / HotChocolate service.
---

# stack-init

Initialize or retrofit a repository so humans and agents know where code belongs, what the domain language means, and which architectural style the project follows. The output is not just docs: when the user wants scaffolding, create the starter project shape that matches the chosen style and make `ARCHITECTURE.md` / `DOMAIN.md` the source of truth for future agentic coding.

## Instructions

1. **Inspect before asking.** Read existing `ARCHITECTURE.md`, `DOMAIN.md`, `AGENTS.md`, `.cursor/rules/**`, `.claude/rules/**`, solution files, project files, package manifests, schema files, and the top-level `src/` / `test/` layout before asking the first setup question.
   - Detect `*.Domain`, `*.Application`, `*.Infrastructure`, `*.GraphQL`, `*.Web`, and `*.Host` projects.
   - Detect `Features/**`, `Commands/**`, `Queries/**`, `Ports/**`, `Adapters/**`, bounded-context folders, EF migrations/configurations, HotChocolate attributes, `.graphql` SDL files, DataLoaders, mediator handlers, and domain events.
   - If the repo is empty, say that recommendations are based on the user's goal rather than existing code.

2. **Build an evidence-backed recommendation.** Load [`references/STYLE-SELECTION.md`](references/STYLE-SELECTION.md) and map the detected signals to candidate styles.
   - Recommend one primary style and, when useful, one overlay. Example: "Clean Architecture with DDD modeling" or "GraphQL-first delivery over Vertical Slice application code".
   - Explain the recommendation with concrete repo evidence: paths, project names, files, package references, or absence of structure.
   - If the user's preferred style conflicts with the existing repo, explain the migration cost before asking for confirmation.

3. **Run the setup questionnaire.** Ask one question at a time. For every question, include:
   - **Observed:** what the repo already uses.
   - **Recommendation:** the option you would pick.
   - **Why:** the trade-off in one or two sentences.
   - **Question:** a concise multiple-choice question.

   Ask only questions that change the generated output:
   1. Architecture style: Clean Architecture, Vertical Slice, GraphQL-first, Hexagonal, DDD overlay, or document existing shape.
   2. Delivery surface: GraphQL, REST/minimal API, message bus/worker, CLI, or multiple adapters.
   3. Domain depth: CRUD/anemic model, rich aggregates, bounded contexts, or event-driven domain.
   4. Persistence and integration defaults: EF Core, Dapper/raw SQL, in-memory/prototype, external services, or no persistence yet.
   5. Bootstrap scope: docs only, docs plus folders/projects, or retrofit existing projects without moving files.

   If a question is already answered by the codebase, still show the observed evidence and ask for confirmation rather than asking blind.

4. **Load the chosen style material.** Read the style reference and inspect the matching reference project before generating files:

   | Style | Reference | Reference project |
   |---|---|---|
   | Clean Architecture | [`references/CLEAN-ARCHITECTURE.md`](references/CLEAN-ARCHITECTURE.md) | `assets/reference-projects/clean-architecture/` |
   | Vertical Slice | [`references/VERTICAL-SLICE.md`](references/VERTICAL-SLICE.md) | `assets/reference-projects/vertical-slice/` |
   | GraphQL-first | [`references/GRAPHQL-FIRST.md`](references/GRAPHQL-FIRST.md) | `assets/reference-projects/graphql-first/` |
   | Hexagonal | [`references/HEXAGONAL.md`](references/HEXAGONAL.md) | `assets/reference-projects/hexagonal/` |
   | DDD overlay | [`references/DDD.md`](references/DDD.md) | `assets/reference-projects/domain-driven-design/` |

   DDD is usually an overlay, not a replacement for delivery/application structure. Combine it with Clean Architecture, Hexagonal, or GraphQL-first when bounded contexts and aggregates are the main design pressure.

5. **Scaffold only after confirmation.**
   - Never overwrite existing `ARCHITECTURE.md`, `DOMAIN.md`, `.csproj`, source folders, or solution files without explicit confirmation.
   - For an empty repo, create the minimal project/folder skeleton from the reference project and rename namespaces/projects to the target repo.
   - For an existing repo, prefer adding missing folders and docs over moving files. If a move is needed, propose a migration plan first.
   - Keep scaffolds agent-friendly: one obvious place for entities, use cases, API surface, persistence, tests, and cross-cutting rules.

6. **Generate `ARCHITECTURE.md`.** Fill [`references/ARCHITECTURE-TEMPLATE.md`](references/ARCHITECTURE-TEMPLATE.md) using real repo paths. Required sections:
   1. Pattern and rationale.
   2. Setup decision log from the questionnaire.
   3. Layers, slices, ports/adapters, or schema boundaries with actual project names.
   4. Dependency rules with a diagram and enforceable `.csproj` / folder rules.
   5. Folder layout copied from the actual repo or scaffold.
   6. Naming conventions.
   7. Cross-cutting concerns.
   8. Agentic coding contract: which docs to read first, where to add a feature, what not to invent, and how to update docs after a change.
   9. How to add a new feature.

7. **Generate `DOMAIN.md`.** Fill [`references/DOMAIN-TEMPLATE.md`](references/DOMAIN-TEMPLATE.md) from the code and the questionnaire.
   - List bounded contexts, aggregates, entities, value objects, domain events, invariants, and ubiquitous language.
   - If no domain exists yet, create explicit TODO markers that say what the first feature must fill in.
   - Make aggregate boundaries and naming rules concrete enough that an agent can add the next entity without asking where it belongs.

8. **Verify and report.**
   - Re-read the generated/updated docs and any scaffolded files.
   - Report the selected style, the recommendation evidence, files created/updated, and any TODOs left intentionally.
   - Do not leave blank sections. Fill them or mark them TODO with a reason.

## Worked example - empty HotChocolate service

User says: "Bootstrap this new service for agentic coding."

**Inspect.** No `src/`, no docs, no schema. The user mentioned HotChocolate in the request.

**Questionnaire.**

> Observed: empty repo; no existing layering to preserve. The goal is a GraphQL service.
> Recommendation: GraphQL-first over Clean Architecture. Start from the GraphQL contract, keep resolvers thin, and put behavior in Application/Domain so future agents do not write business logic in resolvers.
> Why: GraphQL is the public product boundary, but Clean Architecture keeps the domain testable once the schema is approved.
> Question: Which setup should I scaffold?
> 1. GraphQL-first over Clean Architecture (recommended)
> 2. Clean Architecture without GraphQL-first workflow
> 3. Vertical Slice with GraphQL endpoints
> 4. Docs only

**Scaffold.** After confirmation, copy the relevant shape from `assets/reference-projects/graphql-first/`, rename `Reference.GraphQLFirst` to the target namespace, and create `ARCHITECTURE.md` / `DOMAIN.md`.

**Verify.** Report: "Selected GraphQL-first over Clean Architecture because this is an empty HotChocolate service and the schema will drive client work. Created `src/<Name>.GraphQL`, `src/<Name>.Application`, `src/<Name>.Domain`, `src/<Name>.Infrastructure`, `ARCHITECTURE.md`, and `DOMAIN.md`. `DOMAIN.md` has TODOs for the first aggregate because no domain model exists yet."

## Gotchas

- **Do not ask blind questions.** If the repo has `src/Billing.Domain` and `src/Billing.Application`, do not ask "are you using layers?" Say what you found and ask whether to preserve it.
- **Do not force Clean Architecture everywhere.** Vertical Slice is better for simple feature-heavy CRUD. Hexagonal is better when many adapters drive the same core. GraphQL-first is better when the schema contract leads product design.
- **Do not treat DDD as a folder layout only.** DDD without aggregates, invariants, ubiquitous language, and bounded contexts is just renamed folders.
- **Do not scaffold speculative infrastructure.** If no persistence choice is made, create interfaces/placeholders and document the pending decision instead of adding EF Core by default.
- **Existing docs are user-owned.** Read and update them surgically; never replace them wholesale unless the user asks for a rewrite.

## References

- [Style selection](references/STYLE-SELECTION.md) - questionnaire, detection signals, recommendation rules.
- [Clean Architecture](references/CLEAN-ARCHITECTURE.md) - Domain/Application/Infrastructure/Presentation layering.
- [Vertical Slice](references/VERTICAL-SLICE.md) - feature folders and colocated behavior.
- [GraphQL-first](references/GRAPHQL-FIRST.md) - schema-led HotChocolate services.
- [Hexagonal](references/HEXAGONAL.md) - ports and adapters around a core.
- [DDD](references/DDD.md) - bounded contexts, aggregates, value objects, domain events.
- [ARCHITECTURE template](references/ARCHITECTURE-TEMPLATE.md) - fill this in.
- [DOMAIN template](references/DOMAIN-TEMPLATE.md) - fill this in.
