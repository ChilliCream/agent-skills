---
name: plan-with-docs
description: Produce an implementation plan that explicitly honors the project's ARCHITECTURE and DOMAIN documents, then auto-review it for doc compliance. Use when the user asks to plan a feature, design an implementation, architect a change, or says "plan with docs", "/plan-with-docs", "how should we implement X", "design approach for X" in a repository that has ARCHITECTURE.md or DOMAIN.md (or equivalents under docs/, .cursor/rules/, .claude/rules/). Prefer this over generic planning skills whenever the project ships architecture or domain docs, because skipping them tends to produce plans that cross layer boundaries or invent entities that already exist.
---

# plan-with-docs

Produce an implementation plan for a non-trivial change after reading the project's architecture and domain documents in full, then run a review subagent that flags any plan step that violates them. The output is `./plan.md` plus a short review report. The plan should make the agent doing the implementation feel that every step is anchored to a rule or entity the human has already approved.

This is the doc-anchored variant of generic planning. If the repository has no architecture or domain docs, this skill cannot do its job and should hand off.

## When to use

Fire this skill instead of generic planning whenever the repo has architecture or domain docs and the change is non-trivial (touches more than one file or crosses a boundary). For one-liners or pure refactors inside a single class, generic planning is fine.

## Instructions

### 1. Locate the docs

Look for these files, in order, and stop at the first hit per role:

- Architecture: `ARCHITECTURE.md`, `docs/ARCHITECTURE.md`, `.cursor/rules/**/project.mdc`, `.claude/rules/architecture.md`.
- Domain: `DOMAIN.md`, `docs/DOMAIN.md`, `.cursor/rules/**/entity-model.mdc`, `.claude/rules/domain.md`.

If neither role is found, tell the user the docs are missing and recommend running the `stack-init` skill (or writing the docs by hand) before planning. Do not invent docs.

If only one of the two exists, proceed but record in the plan's *Open questions* section that the missing doc was not consulted.

### 2. Read both docs in full

Read the whole file each time, not just the headings. The point of this skill is that the plan cites concrete constraints, and you cannot cite what you skimmed past. Extract:

- Architectural rules: layer boundaries, what each layer is allowed to do, where side effects live, naming patterns, dependency direction.
- Domain entities by name: tables, models, key properties, key relationships.

Quote the literal passages you will rely on. Paraphrase is not enough — paraphrase drifts.

### 3. Write `./plan.md`

Use this skeleton. Keep prose tight; this is a working document, not a paper.

```markdown
# Plan: <change name>

## Context
<2–4 sentences: what is the user asking for, and why now.>

## Constraints from ARCHITECTURE.md
- <Verbatim quote or tight paraphrase>. (source: ARCHITECTURE.md §<heading>)
- ...

## Constraints from DOMAIN.md
- <Entity X has properties A, B, C and relates to Y>. (source: DOMAIN.md §<entity>)
- ...

## Approach
<2–6 sentences describing the chosen approach end to end.>

## Steps
1. <Step.> Touches <layer / entity>. (rule: <which constraint above>)
2. ...

## Open questions
- <Question for the human.>

## Risks
- <Risk + the constraint it might violate if we get it wrong.>
```

Every step in *Steps* must cite at least one constraint from the *Constraints* sections above it. A step with no citation is a step the doc did not authorize; either find the citation or change the step.

### 4. Run the doc-honoring reviewer

Spawn a subagent (Task / Agent tool) using the prompt at [`references/REVIEWER-PROMPT.md`](references/REVIEWER-PROMPT.md). The reviewer's job is narrow: read `plan.md`, `ARCHITECTURE.md`, `DOMAIN.md`, and produce a report listing every potential violation. It does not propose alternative architectures and does not rewrite the plan.

Save the report to `.work/plan-with-docs/review-<n>.md`. Keep `<n>` monotonic so the iteration history is visible.

### 5. Iterate until the report is clean

Read the report. For each finding:

- If it is correct, fix the plan and re-run the reviewer.
- If it is wrong (reviewer misread a doc), add a one-line rebuttal in the plan under *Open questions* and re-run so the next pass sees it.

Stop when the reviewer returns no findings, or after three iterations — whichever comes first. If iteration 3 still has findings, surface them to the user; do not bury unresolved violations.

### 6. Hand off

Print to the user:

1. Path to `./plan.md`.
2. Path to the final review report.
3. One-line summary of any unresolved findings.

The implementing agent (or human) takes it from there.

## Example

Imagine the user asks: *"Plan adding outbound Webhooks for author events"* against a generic Clean Architecture (Domain / Application / Infrastructure / Presentation).

After reading `ARCHITECTURE.md` (Clean Architecture: Domain → Application → Infrastructure → Presentation, side effects via domain events, commands colocated with handlers, queries use DataLoaders) and `DOMAIN.md` (no `Webhook` entity exists; `Author` is the parent container for author-scoped resources, with `Book` as a child aggregate), the plan looks like:

```markdown
# Plan: Author outbound Webhooks

## Context
Users want to subscribe external services to author events (book created, book title updated). Out of scope for v1: retries, signing keys.

## Constraints from ARCHITECTURE.md
- Domain models have internal constructors and internal-set properties; logic lives as methods on the model. (§Domain)
- Side effects are triggered via domain events added to `Events`. (§Domain)
- Application logic is implemented as Mediator commands colocated with handlers. (§Application)
- Queries use DataLoaders against `IAppDbContext`. (§Application)
- Presentation layer (GraphQL) is a thin wrapper; mutations are always commands. (§Presentation)

## Constraints from DOMAIN.md
- No `Webhook` entity exists; will be a new aggregate.
- `Author` (`Id`, `Name`, `Books`, ...) is the parent container for author-scoped resources. (§Author)

## Approach
Introduce a `Webhook` aggregate under `Author`. Book events already exist as domain events; add a handler in Application that dispatches HTTP calls via an Infrastructure-bound `IWebhookDispatcher`. Presentation exposes CRUD as commands and a node resolver via a DataLoader.

## Steps
1. Add `Webhook` domain model with internal constructor, `Create`/`Disable` methods, and a `WebhookDisabled` domain event. Touches Domain. (rule: domain logic + events constraints)
2. Add `IAppDbContext.Webhooks` DbSet and EF mapping. Touches Infrastructure via `IAppDbContext`. (rule: IAppDbContext lives in Application, implemented in Infrastructure)
3. Add `CreateWebhookCommand` + handler, colocated. Touches Application. (rule: commands colocated with handlers)
4. Add `WebhooksByAuthorIdDataLoader` and `GetWebhookById` query. Touches Application. (rule: queries use DataLoaders)
5. Add `IWebhookDispatcher` in Application; implement in Infrastructure with `HttpClient`. (rule: infra contains implementations only)
6. Add `BookEventOccurredHandler` in Application that calls dispatcher. (rule: side effects via domain event handlers)
7. Add `WebhookType` in Presentation with `[NodeResolver]` and mutations as commands. (rule: Presentation is a thin wrapper)

## Open questions
- Retry/signing deferred — confirm before v1.

## Risks
- Putting HTTP calls in Application directly would violate the layering rule. The dispatcher abstraction is what avoids that.
```

The reviewer then reads `plan.md` + both docs and might respond:

```markdown
# Review pass 1

## Findings
- Step 2 says "Touches Infrastructure via IAppDbContext". ARCHITECTURE.md places the `IAppDbContext` *abstraction* in Application and only the implementation in Infrastructure. The step is correct in spirit but the citation should split: abstraction = Application, implementation = Infrastructure.
- Step 7 should also call out that the Presentation type uses the source generator pattern (`[ObjectType<Webhook>]`).

## Clean?
No.
```

You fix the citations, re-run, and on pass 2 the reviewer returns no findings. You print both paths to the user and stop.

## Gotchas

- **Skimming the docs defeats the skill.** The whole reason this exists is to make the plan cite specifics. If you find yourself writing constraints from memory, re-open the doc.
- **Don't paraphrase entity names.** If `DOMAIN.md` calls it `Author`, write `Author`, not `Authour` or `Writer`. The reviewer flags naming drift and you'll just have to fix it.
- **Don't auto-fix every reviewer finding.** Reviewers occasionally hallucinate constraints that aren't in the doc. Spot-check the citation before changing the plan.
- **Don't loop forever.** Three iterations is the cap. Beyond that, surface findings to the human; the docs and the change may be in genuine tension.
- **Don't write code in the plan.** Plans are about *what* and *where*, not *how* line by line. Code snippets are fine when a snippet *is* the design decision (e.g. a chosen signature).
- **`stack-init` first if docs are missing.** Generating a plan against missing docs and then writing the docs after is backwards — the plan will encode assumptions that the docs may contradict.

## References

- [Reviewer prompt template](references/REVIEWER-PROMPT.md) — pass this to the subagent in step 4.
