---
name: discuss
description: Interview the user about a plan or design until shared understanding is reached, walking the decision tree branch by branch and grounding every question in ARCHITECTURE.md / DOMAIN.md / plan.md before asking. Use when the user says "discuss", "grill me", "stress-test this plan", "interview me about this design", "/discuss", or asks you to pressure-test an in-flight design. Distinct from generic Q&A: this skill is opinionated, asks sharp branch-resolving questions, and always offers a recommendation grounded in the project's docs or codebase.
---

# discuss

Interview the user about a plan or design until you both share the same picture of the work. Walk the decision tree one branch at a time. For every branch, lead with a recommendation grounded in the project's `ARCHITECTURE.md`, `DOMAIN.md`, in-flight `plan.md`, or the codebase — then ask a sharp question to confirm or redirect.

This skill builds on the upstream `grill-me` pattern (Matt Pocock's "grill me with docs"): relentless interview, decision-tree resolution, recommendation-first questions. What this skill adds is **docs-grounding**: load the architecture and domain docs first, and let them shape both the questions you ask and the recommendations you give. The point is to avoid asking from a blank slate.

## When to use

- The user is mid-design and wants the plan stress-tested.
- A `plan.md` exists and the user wants the gaps surfaced before implementation.
- The user explicitly invokes `/discuss`, says "grill me", or asks to be interviewed.

Do not use for simple factual Q&A ("how does X work?") — the skill is for resolving open design decisions, not explaining existing systems.

## Instructions

### 1. Load context before opening your mouth

Read, in this order, whatever exists:

1. `ARCHITECTURE.md` (or equivalent: `AGENTS.md`, repo-root architecture docs, `.cursor/rules/**`) — the system's structural rules.
2. `DOMAIN.md` (or equivalent: ER diagrams, schema docs, entity model notes) — entities, relationships, invariants.
3. `plan.md` or whatever the user is asking you to stress-test.
4. Any obviously relevant code (existing similar features, the layer that will host the new work).

If none of the docs exist, tell the user up front:

> No ARCHITECTURE.md / DOMAIN.md found. I'll ground questions in the codebase instead — expect this to be less precise.

Then proceed with codebase exploration.

### 2. Map the decision tree before asking anything

From the plan or design, list every decision branch the work touches. Typical axes:

- **Layer placement** — domain vs. application vs. infrastructure vs. API surface.
- **Entity shape** — new aggregate vs. extending an existing one; properties; relationships; cascades.
- **Persistence** — schema migrations, indexes, soft-delete vs. hard-delete, ownership boundaries.
- **Cross-cutting concerns** — auth (who can see/modify), audit logging, eventing (domain events vs. integration events), pagination, observability.
- **API surface** — types/mutations/endpoints, naming conventions, command shape, resolver/handler placement.
- **Failure modes** — concurrency control, retries, idempotency, partial failure.
- **Operational** — deployment, feature flags, migration rollout.

Hold this list privately. Don't dump it on the user. You'll walk it with them.

### 3. Walk the tree one branch at a time

For each branch, in dependency order (resolve foundational decisions before leaf ones):

1. **State the recommendation first.** Cite the doc or code that justifies it.
2. **Ask a sharp, specific, branch-resolving question.** Yes/no or pick-one-of-N when possible. No open-ended "what do you think about persistence?" Instead: *"Should `Review` be its own aggregate, or a value object inside `Book`?"*
3. **Wait for the answer.** Don't pile three questions on one turn — that bypasses the resolve-before-moving-on rule.
4. **Resolve dependencies before moving on.** If the user picks option A, only then ask the question that only matters under A.

One question per turn. Two at most, and only when they are genuinely independent.

### 4. Prefer the codebase over the user

If a question has an answer in the docs or code, find it and confirm — don't ask blind. Example:

- Wrong: *"How does this project handle audit logging?"*
- Right: *"`DOMAIN.md` describes an audit-log table keyed by aggregate id and discriminator. I'll plan a new discriminator value for this change type and log inserts/deletes there. Confirm?"*

This is the docs-grounding the skill exists for. Skip it and you've degraded to plain `grill-me`.

### 5. Track open issues

Keep a running list of unresolved or deferred branches. At the end of the session (or whenever the user calls time):

- List each unresolved branch with your recommended resolution.
- Note which decisions the user explicitly deferred and what triggers picking them back up.

### 6. Stop when shared understanding is reached

"Shared understanding" = every branch in your tree is either resolved or explicitly deferred, and you can restate the plan to the user without using the word "probably". Then stop. Don't keep asking.

## Question style

- **Recommendation-first.** Lead with your call; ask for confirmation or redirection. The user shouldn't have to guess what you think.
- **Specific.** Name the entity, the layer, the file. *"Should the handler live in `app/reviews/create_review.py`?"* beats *"Where should the handler go?"*
- **Decidable.** A good question has a discrete answer set. Avoid *"how should we think about X"*.
- **Grounded.** Every recommendation cites something — a doc section, a similar entity, a convention.
- **One at a time.** Resolving in dependency order is the whole point. Piled questions defeat that.

## Worked example: grilling an "add Webhooks" plan

Assume the user's plan is *"Add webhooks so a user can be notified when a new `Book` is published by an `Author` they follow."* Architecture and domain docs exist at `ARCHITECTURE.md` and `DOMAIN.md`.

**Turn 1 — ownership branch (foundational, must resolve first):**

> `DOMAIN.md` shows two existing scoping patterns: `Book` is scoped to `Author` (one author, many books); user `Subscriptions` are scoped to the `User` aggregate. Publish events fire per-book — the existing `BookPublished` domain event carries an `AuthorId`. A user-scoped webhook would have to filter publish events against the user's follow list at delivery time; an author-scoped webhook (one webhook per author the user wants to track) matches the event source one-to-one.
>
> **Recommendation:** make `Webhook` its own aggregate keyed by `UserId` + `AuthorId` (mirrors how `Subscription` is shaped in `DOMAIN.md`). Property set: `Id`, `UserId`, `AuthorId`, `Url`, `Secret` (hashed — find the closest existing aggregate that stores credentials and copy its hashing approach), `EventTypes` (e.g. `["book.published"]`), `IsActive`, `CreatedAt`.
>
> Question: scope `Webhook` per-`(User, Author)` pair, or do you need a single user-level webhook that fans out across all followed authors?

(Wait for answer. Suppose the user picks per-author.)

**Turn 2 — command shape & layer placement:**

> `ARCHITECTURE.md` says all mutations are commands colocated with their handler in the application layer, validated against the current session, then persisted through the aggregate's repository. Look at the closest existing write command in the codebase (e.g. whatever creates a `Subscription`) and copy its shape: command record holds the inputs, handler resolves the `Author`, constructs the `Webhook` via a static factory on the aggregate, and saves through the repository. The plaintext secret should be returned exactly once via a wrapper type — find a similar one-shot-secret pattern already in the codebase.
>
> **Recommendation:** `CreateWebhookCommand(session, authorId, url, eventTypes) → WebhookWithSecret`. Handler in `app/webhooks/`. Domain aggregate `Webhook` in `domain/webhooks/` with internal constructor and `Webhook.Create(...)` factory that returns both the persisted entity and the plaintext secret.
>
> Question: any reason to deviate from the existing one-shot-secret pattern — e.g. should the secret be user-supplied instead of generated?

(Wait. Suppose the user says generated.)

**Turn 3 — eventing & side effects:**

> Side effects in this project are domain events raised on the aggregate and handled by application-layer subscribers. Webhook *delivery* is itself a side effect of the existing `BookPublished` event. A subscriber on `BookPublished` should query active webhooks for the book's `AuthorId`, enqueue an outbound HTTP call per match, and write an audit-log entry for each delivery attempt.
>
> **Recommendation:** add a `WebhookDelivered` audit-log entry type; deliver asynchronously via the project's existing background-job mechanism (whatever's documented in `ARCHITECTURE.md` for async work) rather than synchronously in the handler, so a slow customer endpoint doesn't block the publish flow.
>
> Question: synchronous fire-and-log (simpler, blocks publish completion) or queued via the background-job mechanism (recommended; matches existing async patterns)?

Three turns in, the foundational branches (ownership, command shape, delivery mode) are resolved. Remaining branches (retry policy, signature header format, API surface exposure, pagination on `webhooks` field) are leaves — ask them next, one per turn.

## Gotchas

- **Don't ask everything up front.** A wall of seven questions is not a grill, it's a survey. Resolving in dependency order is the whole point.
- **Don't ask blind when the docs answer it.** Loading `DOMAIN.md` and then asking *"what entities does this project have?"* is malpractice. Read, then confirm.
- **Don't accept "yeah sounds good" on a foundational branch.** If the user rubber-stamps without engaging, probe: *"Specifically, the part about scoping per-`(User, Author)` rather than per-`User` — that locks us out of a single fan-out webhook. Acceptable?"*
- **Don't moralize or hedge.** State the call. *"Recommendation: scope to `Author`."* not *"It might be worth considering whether perhaps scoping to `Author` could be one approach to think about."*
- **Don't grill past the point of resolution.** Once every branch is resolved or deferred, summarize and stop. Continuing past that wastes the user's time and signals you can't tell when you're done.
- **Don't invent docs.** If `ARCHITECTURE.md` doesn't say something, say so — don't fabricate a convention to justify a recommendation. Fall back to "the closest existing pattern is X" and cite the file.

## References

- Upstream pattern: Matt Pocock's "grill me with docs" / the `grill-me` skill. This skill is the docs-grounded variant — same interrogation cadence, but every recommendation must cite a doc or a code file.
- Typical architecture doc shapes this skill consumes: a repo-root `ARCHITECTURE.md` (or `AGENTS.md`, `.cursor/rules/**`) describing layer/module conventions, plus a `DOMAIN.md` (or equivalent entity-model / schema doc) describing aggregates and their relationships.
