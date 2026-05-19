<!--
DOMAIN.md template. Copy this to the target repo's root and replace every
{placeholder}. Leave a section as a TODO marker if you genuinely cannot fill it
in (e.g., no entities exist yet), and report the TODO back to the user.
-->

# Domain model

## Domain discovery notes

{Summarize whether this domain was discovered from existing code, created for a new repo, or left as TODO because no domain model exists yet. Include the architecture style chosen in `ARCHITECTURE.md`.}

## Bounded contexts

<!-- Delete this section if the codebase has only one bounded context. -->

| Context | Purpose | Upstream | Downstream | Integration style |
| ------- | ------- | -------- | ---------- | ----------------- |
| {Name}  | {one-sentence purpose} | {who feeds this context} | {who consumes from this context} | {shared kernel / customer-supplier / ACL} |

## Aggregates and entities

| Name | Aggregate root? | Owns | Notes |
| ---- | --------------- | ---- | ----- |
| {EntityName} | yes / no | {child entities, comma-separated} | {one-line purpose} |
| {EntityName} | yes / no | — | {one-line purpose} |

<!-- Example row, delete when filling in:
| Author | yes | Books | Person responsible for one or more books; aggregate root that enforces invariants across the author's catalogue. |
| Book   | no  | —     | Title authored by an Author; has its own identity but lives inside the Author aggregate. |
-->

## Value objects

| Name | Fields | Purpose |
| ---- | ------ | ------- |
| {ValueObject} | {Field1, Field2} | {what it represents} |

<!-- Example:
| Isbn       | Value                  | A 13-character book identifier; validated for format and checksum on construction. |
| AuthorName | First, Last            | An author's display name; cannot be empty. |
-->

## Domain events

| Event | Raised by | When |
| ----- | --------- | ---- |
| {EntityCreatedEvent} | {Entity.Create} | {after a new instance is created} |
| {EntityRenamedEvent} | {Entity.Rename} | {after Name changes} |
| {EntityRemovedEvent} | {Entity.OnRemove} | {when the entity is removed from the context} |

## Invariants

Rules the domain enforces. Each is implemented inside the relevant entity method — bypassing them is not possible from outside the assembly.

- **{Entity}.{Method}** — {rule, e.g., "Name cannot be empty or whitespace."}
- **{Entity}.{Method}** — {rule, e.g., "Cannot transition from Cancelled back to Active."}
- **{Aggregate}** — {rule that spans entities inside the aggregate, e.g., "Total must equal the sum of line item amounts."}

## Ubiquitous language

Glossary of domain terms. Match the spelling and meaning in code, in this document, and in conversation with domain experts.

| Term | Meaning |
| ---- | ------- |
| {Term} | {definition; if the same word means different things in different contexts, list each.} |

<!-- Example:
| Author | A person who writes Books. Aggregate root; owns the lifecycle of every Book attributed to them. |
| Book   | A titled work authored by an Author. Has its own identity but cannot exist without an Author. |
-->

## Agentic domain contract

Agents making domain changes must:

1. Reuse the terms in this glossary exactly.
2. Add new behavior through aggregate methods/factories when the domain has rich invariants.
3. Reference other aggregates by id unless this document says the entity is owned by the same aggregate.
4. Add or update domain events in this document when behavior emits them.
5. Add TODO markers with a reason when the first implementation cannot yet define a concept.

Do not create a new entity, value object, event, or bounded context without updating this file in the same change.

## See also

- [`ARCHITECTURE.md`](./ARCHITECTURE.md) — layers, dependency rules, and where each kind of code lives.
