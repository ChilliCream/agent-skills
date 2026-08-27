---
name: nitro-wayfinder
description: Plan a large, fuzzy feature as a shared map of decision tickets in nitro agent tasks, resolve them one per session until the way is clear, then cut the implementation-ready task graph. Use when the user says "wayfind", "chart a map", "work the map", "next decision", "plan this like a map", invokes /nitro-wayfinder, or brings an effort too big and foggy for one agent session. Not for work that fits one session; create tasks with nitro agent tasks directly for that (see the nitro-task skill).
---

# Wayfinding with nitro agent tasks

A loose idea has arrived: too big for one agent session, and wrapped in fog, so the way from here to the **destination** is not visible yet. Wayfinding finds that way instead of charging at the destination. You chart a **map** in `nitro agent tasks`, then work its **decision tickets** (questions whose resolution is a decision, not slices of a build) one at a time until nothing is left to decide. Then you cut the implementation tasks that carry the result to the build. If `nitro` itself is not found, the CLI is not installed — stop and tell the user to install it: https://chillicream.com/docs/nitro/cli/installation. Do not attempt to install it yourself.

The destination varies per effort, and naming it is the first act: it fixes the scope every ticket is measured against. It is usually a feature or change ready to be cut into implementation tasks; it can also be a design or migration plan that those tasks then execute.

## Core principles

- **nitro agent is the only memory.** Sessions die and compact; the workspace survives. Every decision lives in exactly one closed ticket; the map only indexes it. Standing preferences for the effort go to `nitro agent memory`, the workspace-wide store every agent here reads (see the nitro-memory skill); coordination with other sessions goes over `nitro agent mail`. Never rely on chat context for anything a future session needs.
- **Plan, don't do.** Each ticket resolves a decision. The pull to just build something is the signal that you have reached the edge of the map and it is time to hand off (see [references/handoff.md](references/handoff.md)). Nothing in a map's **Notes** section can license execution; building always happens in a separate session, from implementation tasks.
- **One decision per session.** Resolve one ticket, do the graduation pass, stop. Chaining into the next decision is how context quality degrades. Research tickets are the exception: they run as subagents, in parallel, alongside the one decision.
- **The map is an index, not a store.** It gists and links; the ticket holds the detail. A session loads the map at low resolution and zooms into tickets on demand with `nitro agent tasks show <id> --output json`.
- **Refer by name.** In everything the human reads, call tickets by their title, with the id in parentheses: "Which export format? (bill-3f2.1)". A wall of bare ids is illegible.
- **HITL tickets need the human.** HITL (human in the loop) tickets resolve only through a live exchange; AFK (away from keyboard) tickets are driven by the agent alone. An agent that answers its own questions has broken the loop.

Command mechanics for tasks, mail, and memory live in the nitro-task, nitro-mail, and nitro-memory skills. This skill covers how they compose; the exact commands for each wayfinding operation are in [references/operations.md](references/operations.md).

## The map

The map is one task of type `epic`, labelled `wayfinder:map`, titled `Map: <effort>`. Its description is the low-resolution view every session loads first. A filled-in example:

```markdown
## Destination
Finance can download invoices as a file their tools import, and a nightly job writes the same file to shared storage.

## Notes
Repo: billing (.NET). Memory tag: wayfinder-billing-export.
Tickets under this map carry wayfinder:* labels; they are decisions, never build work.
Prefer boring formats; finance runs Excel on Windows.

## Decisions so far
- Which export format? (bill-3f2.1): CSV per RFC 4180, header row fixed, see ticket for columns

## Not yet specified
- retention of old exports; depends on where they land
- who may trigger a manual export

## Out of scope
- multi-currency totals: past the destination, finance reconciles per currency today (bill-3f2.5)
```

Open tickets are not listed in the map. They are its children, found by query.

## Decision tickets

Every ticket is a child of the map (`--parent <map-id>`), so it gets an id like `<map-id>.3` and shows up under the map. Its description is the question, sized to one agent session, with enough context to be answered cold:

```markdown
## Question
Which file format do exported invoices use, and who consumes it? Finance imports into Excel; a reconciliation job reads the same file nightly. Candidates: CSV, JSON lines, PDF bundle.
```

Each ticket carries one wayfinder label, `wayfinder:grilling`, `wayfinder:research`, `wayfinder:prototype`, or `wayfinder:task`. Decision tickets are `--type question`; task tickets are `--type task`.

| Label | Mode | Resolves by | Reference |
|---|---|---|---|
| grilling | HITL | rounds of numbered questions, each with a recommendation; the default | [references/grilling.md](references/grilling.md) |
| research | AFK | a subagent reading primary sources and reporting facts | [references/research.md](references/research.md) |
| prototype | HITL | a throwaway artifact the human reacts to; may falsify a design | [references/prototype.md](references/prototype.md) |
| task | either | manual work that must happen before a decision can be made; it earns its place only by unblocking a decision | [references/operations.md](references/operations.md) |

Blocking uses native dependencies (`--depends-on`, `nitro agent tasks dep add`). A ticket is unblocked when every ticket it depends on is closed. The **frontier** is the set of open, unblocked, unclaimed children: `nitro agent tasks ready`, filtered to the map's children (the query is in operations.md), shows exactly that. The parent edge never blocks a child; it only keeps the map from being finished while children are open.

The answer is not part of the ticket body. It is recorded on resolution as a comment, and the ticket is closed.

## Fog of war and out of scope

The map is deliberately incomplete. Beyond the live tickets lies the fog: decisions you can tell are coming but cannot pin down yet, because they hang on open questions. Write them into **Not yet specified** as loosely as the view allows. Resolving a ticket clears the fog ahead of it; graduate what became sharp into new tickets and delete the graduated patch from the fog, so it lives only as its ticket.

**Fog or ticket?** Ticket when you can state the question precisely now, even if it is blocked. Fog when you cannot phrase it that sharply yet. Do not pre-slice fog into ticket-sized pieces; one patch may become several tickets or none.

Work beyond the destination is **out of scope**, not fog. When a ticket turns out to sit past the destination, close it with a reason and leave one line in **Out of scope**. It never graduates and never appears in **Decisions so far**; it returns only if the destination is redrawn, as a new effort.

## Invocation

Both modes start with the session hygiene in [references/operations.md](references/operations.md): take the planner role under the actor name your context states, drain unread mail, load the effort's memory. Loaded memory carries the effort's **question style**; if none is saved, agree it before the first question (see [references/grilling.md](references/grilling.md)) and save it, so later sessions inherit it and never re-ask.

### Chart the map

The user arrives with a loose idea.

1. **Agree the question style.** Ask once, before any other question: batched rounds, or one at a time in Issue / Example / Recommendation prose (see [references/grilling.md](references/grilling.md)). It shapes every exchange for this effort, so it comes before the first grill.
2. **Name the destination.** Grill until the destination fits in two lines. It fixes the scope, so it is settled first.
3. **Map the frontier.** Grill again, breadth-first: fan out across the whole space, surfacing the open decisions and the fog. If this surfaces no fog and the journey fits one session, the user does not need a map: stop and ask how they want to proceed (usually: cut the tasks directly with the quality bar in [references/handoff.md](references/handoff.md)).
4. **Create the map**, then the tickets you can state now, then wire blocking edges in a second pass (tickets need ids before they can reference each other). Everything you cannot state yet stays in **Not yet specified**.
5. **Save standing preferences** the user expressed, the question style from step 1 among them (`nitro agent memory save ... --type preference --tag <memory tag>`), so future sessions inherit them without rereading the chat.
6. **Resolve the research tickets.** Claim each, dispatch one subagent per ticket in parallel, wait, and record each resolution as it lands. Research is the one ticket type charting resolves.
7. End the session here. Charting resolves only research tickets (step 6); every other ticket waits for its own future session.

### Work through the map ("next")

The user names the map, or just says "next". Without a named map, look it up (`nitro agent tasks list --label wayfinder:map`); with several, ask which. A ticket is optional; without one, you pick.

1. Load the map (`show <map-id>`), then `nitro agent memory context --tag <memory tag>` for the effort's preferences, the question style included. If no style is saved, agree one before the first question and save it.
2. Fire subagents for any research tickets on the frontier (claim each first); they run while you work.
3. Choose the ticket: the one named, else the first frontier ticket by priority, then lowest id. Check it is unclaimed, then **claim it** (`update <id> --claim`) so parallel sessions skip it. If the frontier is empty but tickets remain, stop and report what is blocked or held by whom; never reclaim another session's ticket without the user confirming that session is dead.
4. Resolve it by its type. Zoom as needed: read the closed tickets it depends on in full; the map gives gists, the tickets hold the contracts.
5. Record the resolution: comment the answer, close the ticket, append one line to the map's **Decisions so far**. Record research results the same way as they land.
6. **Graduation pass**, the actual engine of progress: what did this answer unlock? Create the newly statable tickets (create, then wire edges), promote fog that became sharp, close tickets the answer invalidated, rule things out of scope explicitly.
7. End the session. If the map now has no open tickets and no fog, say so: the way is clear, and the next session hands off.

Other sessions may be working the same map concurrently. Expect the tracker to change under you; reload the map before editing it.

## Reaching the destination

When no open tickets remain and **Not yet specified** is empty, the way is clear. The outcome is tasks, not a document: cut the implementation graph under an `epic` per area, each task written to the quality bar in [references/handoff.md](references/handoff.md), wired from foundations to tests, `dep cycles` empty, `lint` clean, and briefed to the orchestrator over mail. Then close the map with a reason that names the implementation epics.

## Reference file index

| Read | When |
|---|---|
| [references/operations.md](references/operations.md) | Any write to the tracker: identity, creating the map or tickets, claiming, resolving, the resolution comment template, editing the map body, the frontier query, memory and mail usage, git rules, session hygiene |
| [references/grilling.md](references/grilling.md) | Resolving a grilling ticket, naming the destination, mapping the frontier: the question style choice, both ask formats, domain modeling, the HITL rules |
| [references/research.md](references/research.md) | Creating or resolving a research ticket: the subagent brief and where findings land |
| [references/prototype.md](references/prototype.md) | A decision hinges on "does this actually work" or "how should this look": building and capturing a throwaway artifact |
| [references/handoff.md](references/handoff.md) | The way is clear: cutting implementation tasks, the quality bar, wiring, verification, and the orchestrator briefing |

## Gotchas

- **Decision tickets that start producing deliverables** mean the map is done in that region. Stop and hand off instead. A task ticket that reads like a slice of the build is mis-typed: close it and let the handoff cut it as an implementation task.
- **Questions without context get rejected.** Every question the human sees must be self-contained: the context inside the question, never "see my reasoning above".
- **Compaction is harmless only if nothing load-bearing lives in chat.** Write the resolution comment before you write the summary for the user.
- **Shell state does not survive between tool calls.** Pass `--actor <name>` and literal ids on every command; exported variables from an earlier call are gone.
- **`update --description` replaces the whole map body.** Read it with `show --output json`, edit, write it back; never write from memory.
- **Never resolve a HITL ticket alone.** If the human is away, resolve research tickets or stop.
