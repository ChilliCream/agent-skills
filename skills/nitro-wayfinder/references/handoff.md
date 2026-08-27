# Handoff: from a cleared map to implementation tasks

When the map has no open tickets and **Not yet specified** is empty, the way is clear. The outcome of wayfinding is not a document: it is an implementation-ready task graph in the same tracker, written so that implementer agents can build from it without the chat that produced it. This session cuts that graph, verifies it, and briefs the orchestrator. If a destination turns out to need nothing built (a pure decision), the closed tickets are the deliverable; close the map and say so.

The orchestrator is a separate session running the sibling nitro-task-orchestrator skill. It takes the role `orchestrator`, groups ready tasks into waves by area label, and finds planners by role `planner`. Roles, not names, are how the two sides find each other: actor names are allocated per session.

## Before writing tasks

- Reread every closed decision ticket in full (`nitro agent tasks show <id> --output json`, including comments). The map's gists are not enough; the comments hold the locked parameters.
- Inspect the code that will change; for UI or behaviour work, look at the running app. Tasks written from memory produce implementer churn.
- Check for overlap: `nitro agent tasks search "<keyword>" --output json` and `nitro agent tasks list --status open --output json`. Update an existing task instead of duplicating it.
- A user ruling made while cutting tasks goes into the affected task as a comment, not only its description. Comments are the decision log implementers read.

## Structure

- One `epic` per code area (`storage`, `api`, `ui`, `jobs`...), titled `[<effort>] <area>`, labelled with the area. Implementation tasks are children of their area epic (`--parent`). The parent edge does not block the children; the orchestrator closes an epic once its children are done (`nitro agent tasks epic status`).
- Order with `blocks` edges from foundations to tests: storage before services, services before API, API before UI, everything before end-to-end tests. Cross-area interactions get an explicit note in the later task ("re-verify what <id> landed").
- Size each task for one implementer agent: one coherent change, verifiable on its own. Anything that needs two code areas becomes two linked tasks.
- Every locked parameter from the decision tickets goes into the task that implements it, by value, with a pointer to the ticket by name and id. Implementers treat the task as authoritative and will not go looking.

## Task quality bar

Every implementation task's description contains:

- **Problem**: what is missing or wrong, with evidence (file path, decision ticket).
- **File scope**: the concrete directories and files the implementer may touch. A boundary, not a hint.
- **Fix direction**: the intended approach, including the locked parameters. "Implement it" is not a plan.
- **Verification**: the commands or checks that prove it works. Name the real test filter or entry path.
- **Non-goals**: what is explicitly out of scope, especially adjacent cleanup an implementer would be tempted to do.

Plus the metadata the orchestrator schedules by: `--priority` 0-4, `--type` (`task`, `bug`, `feature`, `chore`), and the **area label**. An unlabelled task cannot be placed in a wave.

Example, with the jobs epic created as `bill-4a1`:

```bash
nitro agent tasks create "Write invoice CSV exporter" --actor maya \
  --parent bill-4a1 --type feature --priority 1 --label jobs --output json \
  --description "$(cat <<'EOF'
## Problem
Invoices cannot be exported. Decided in "Which export format?" (bill-3f2.1) and "Encoding and line endings" (bill-3f2.4).

## File scope
src/Billing.Jobs/Export/**, src/Billing.Jobs/Export.Tests/**

## Fix direction
CSV per RFC 4180, UTF-8 with BOM, CRLF, header row fixed to: id, issued_at, customer_id, total_cents, currency.
One file per day, named invoices-YYYY-MM-DD.csv, written to the finance share decided in "Where do exports land?" (bill-3f2.2): /finance/exports/invoices/.

## Verification
dotnet test src/Billing.Jobs/Export.Tests --filter Category=Export

## Non-goals
Scheduling ("Export schedule", bill-3f2.7), retention (out of scope on the map), any UI.
EOF
)"
```

## Verify the graph

```bash
nitro agent tasks dep cycles --output json                # {"items":[]}
nitro agent tasks lint --output json                      # no findings on the new tasks
nitro agent tasks ready --output json | jq '[.items[] | select(.id | startswith("bill-4a1."))]'   # per epic: only its foundations
```

Then close the map: `nitro agent tasks close bill-3f2 --actor maya --reason "Way clear; implementation under [billing export] jobs (bill-4a1), [billing export] api (bill-4a2)"`.

## Brief the orchestrator

1. Find it: `nitro agent list --role orchestrator --output json`. If none is registered, report the created tasks to the user and stop; do not become the orchestrator. Mail to an unregistered name is accepted but reaches nobody.
2. Mail a compact briefing to the actor name that query returned, never an assumed one: `nitro agent mail send --to <orchestrator-actor> --actor maya --subject "[plan] billing export" --body "$(cat <<'EOF' ... EOF)"` with the epic and task ids (one line each, by name), area labels, the ordering constraints, any open question that needs a user ruling, and a note that tasks labelled `wayfinder:*` are decision tickets, never wave material. The orchestrator reads details with `show`; do not paste descriptions.
3. The briefing is stored before the orchestrator is woken, so a non-zero exit from `send` means the wake went unconfirmed, not that the mail was lost. If your harness can message another running session, send it a one-line pointer to the mail; otherwise the orchestrator drains its inbox between waves. If it mails back (ambiguous task, scope collision), fix the task and `nitro agent mail reply` on the same thread with what changed.

## What this session never does

Implement, claim or close implementation tasks, run waves, commit, push, switch branches, or touch the orchestrator's environment (dev server, browser, in-flight agents). The tracker is the interface; git state belongs to the user and the orchestrator.
