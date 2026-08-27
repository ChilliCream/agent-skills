# Wayfinding operations in nitro agent

How each wayfinding operation maps onto `nitro agent tasks`, `nitro agent memory`, and `nitro agent mail`. Every command used here supports `--output json`; use it whenever you read a result programmatically. Full command references live in the nitro-task, nitro-mail, and nitro-memory skills.

The examples use one effort throughout: prefix `bill`, map `bill-3f2`, tickets `bill-3f2.1`, `bill-3f2.2`, memory tag `wayfinder-billing-export`, actor `maya`. Substitute the literal ids you got back from `--output json`; never carry shell variables across tool calls, they do not survive.

## Identity

Every write records an actor, and a claim assigns the ticket to that actor. You never choose that name: the session-start hook states it in your context (`Your Nitro actor name is "maya".`). Pass it every time: `--actor maya` on `tasks create`, `update`, `comment add`, `close`, `dep add`, on `memory save` and `memory log`, and on every `mail` command. Read commands (`tasks show`, `ready`, `list`, `memory context`, `search`) take no actor.

If no actor name reached your context, allocate one -- never invent it:

```bash
nitro agent login                              # prints: Your Nitro actor is 'maya'.
```

Then take the role the orchestrator looks for when it wants planners. Repeat `--role` on every register; omitting it writes an empty role:

```bash
nitro agent register --actor maya --role planner
```

If the workspace has no tracker yet, run `nitro agent init` once; it sets up tasks, mail, and memory for the repository, shared across all its git worktrees. How and where state is stored is the CLI's business, not yours.

## Multi-line values

Use a quoted heredoc for every multi-line value (map body, ticket question, resolution comment, mail body). It survives apostrophes and backticks; `$'...'` does not.

```bash
--description "$(cat <<'EOF'
## Question
...
EOF
)"
```

Never leave scratch files in the working tree. Inline heredocs need none.

## Map

Create:

```bash
nitro agent tasks create "Map: billing export" --actor maya \
  --type epic --label wayfinder:map --priority 1 --output json \
  --description "$(cat <<'EOF'
## Destination
Finance can download invoices as a file their tools import, and a nightly job writes the same file to shared storage.

## Notes
Repo: billing (.NET). Memory tag: wayfinder-billing-export.
Tickets under this map carry wayfinder:* labels; they are decisions, never build work.

## Decisions so far

## Not yet specified
- retention of old exports

## Out of scope
EOF
)"
```

Find an existing map: `nitro agent tasks list --label wayfinder:map --output json`. The map owner is the actor in its `createdBy` field.

Load: `nitro agent tasks show bill-3f2 --output json`. The `description` field is the map body; `dependents` lists every child with its status; `comments` may carry rulings from other sessions.

Edit the body (read, modify, write back; `--description` replaces the whole body):

```bash
nitro agent tasks show bill-3f2 --output json | jq -r .description
# compose the new body from what you just read, then:
nitro agent tasks update bill-3f2 --actor maya --description "$(cat <<'EOF'
...the full new body...
EOF
)"
```

Because the map is an `epic`, its `status` stays `open` while children are open, but it is reported as blocked: `show` returns `blockers: ["bill-3f2.2:child-open", ...]`, `nitro agent tasks blocked` lists it, and `nitro agent tasks epic status` shows `isEligibleForClose: false`. That is expected; it means the map is not done. Never set `--status blocked` on it. The CLI does allow closing a map with open children, so "close the map only at handoff" is a rule you keep, not one the tool enforces.

## Decision ticket

Create as a child of the map with one wayfinder label and the question as description:

```bash
nitro agent tasks create "Which export format?" --actor maya \
  --parent bill-3f2 --label wayfinder:grilling --type question --output json \
  --description "$(cat <<'EOF'
## Question
Which file format do exported invoices use, and who consumes it? Finance imports into Excel; a reconciliation job reads the same file nightly. Candidates: CSV, JSON lines, PDF bundle.
EOF
)"
```

The id becomes `bill-3f2.<n>`. `lint` flags open tasks with an empty description, so always write the question. Use `--priority` (0-4) to order the frontier; equal priorities are taken lowest id first.

Blocking, in a second pass once ids exist:

```bash
nitro agent tasks dep add bill-3f2.2 bill-3f2.1 --actor maya   # .2 depends on .1 (type blocks)
nitro agent tasks create "..." --parent bill-3f2 --depends-on bill-3f2.1 ...   # or at creation
nitro agent tasks dep cycles --output json                             # must return {"items":[]}
```

Parent edges do not block children. Only `blocks` dependencies do.

## Frontier

```bash
nitro agent tasks ready --output json \
  | jq '[.items[] | select(.id | startswith("bill-3f2."))] | sort_by(.priority, .id)'
```

`ready` is workspace-wide and already excludes blocked and claimed (`in_progress`) tasks, so the prefix filter yields the frontier: open, unblocked, unclaimed children of this map. First in the sorted list wins unless the user named a ticket. `nitro agent tasks blocked --output json` shows what is waiting and on what.

If the frontier is empty but children remain: `nitro agent tasks list --status in_progress --output json` filtered the same way shows claims held by other sessions; report them and stop. Reclaim (`update <id> --status open --assignee ""`) only when the user confirms that session is dead.

## Claim

The first write of a session, before any work. `--claim` does not refuse a ticket someone else holds, so check first:

```bash
nitro agent tasks show bill-3f2.1 --output json | jq '{status, assignee}'   # open + null: free
nitro agent tasks update bill-3f2.1 --actor maya --claim              # in_progress + assignee = you
```

`in_progress` with another assignee means another session is on it; pick the next frontier ticket.

## Resolve

The resolution comment is the contract future sessions and implementers read. Use this shape:

```markdown
## Decision
CSV per RFC 4180, UTF-8 with BOM, CRLF line endings.

## Rejected
- JSON lines: no consumer today; a second parser for nothing.
- PDF bundle: finance needs cells, not pages.

## Locked parameters
- header row: id, issued_at, customer_id, total_cents, currency
- one file per day, named invoices-YYYY-MM-DD.csv

## Assets
- (path or branch of research findings or prototype, if any)
```

```bash
nitro agent tasks comment add bill-3f2.1 --actor maya "$(cat <<'EOF'
## Decision
...
EOF
)"
nitro agent tasks close bill-3f2.1 --actor maya --reason "Decided: CSV per RFC 4180"
```

Then append one line to the map's **Decisions so far** (see Map above) and do the graduation pass: create newly statable tickets, wire edges, close invalidated tickets with a reason (`--reason "Invalidated by bill-3f2.1: ..."`; prefer close over delete, it keeps the audit trail), delete graduated patches from the fog, move out-of-scope work.

## Out of scope

```bash
nitro agent tasks close bill-3f2.5 --actor maya --reason "Out of scope: past the destination (multi-currency totals)"
```

plus one line under **Out of scope** in the map. Never list it under **Decisions so far**.

## Git rules

The wayfinding session never commits, pushes, or switches branches in the user's working tree. Research findings and prototypes are files; when the repository wants them off the main branch, the subagent that produced them creates a throwaway branch in a separate worktree (`git worktree add ../billing-research-storage -b research/storage`) and commits there, and the ticket links the branch or path. The working branch, main, and pushes belong to the user.

## Memory

Use `nitro agent memory` (mechanics in the nitro-memory skill) for what every future session of this effort must know without rereading tickets: standing preferences and domain facts. The store is workspace-wide, so an orchestrator or planner working the same repo reads what you save here. Decisions themselves stay in tickets. The effort's memory tag is written in the map's Notes; tags and types allow only lowercase letters, digits, and hyphens.

```bash
nitro agent memory save --actor maya --type preference --tag wayfinder-billing-export \
  "Prefer boring formats: CSV over Parquet unless a consumer needs columnar."
nitro agent memory save --actor maya --type preference --tag wayfinder-billing-export \
  "Wayfinder question style: batched rounds via AskUserQuestion."
nitro agent memory context --tag wayfinder-billing-export             # at session start: prompt-ready block
nitro agent memory search "export" --tag wayfinder-billing-export    # when a question smells familiar
```

`save` requires `--type` (`fact`, `decision`, `preference`, `reference`). Memories are shared with every agent in the workspace. `memory log` is a cheap journal for a session's loose ends; promote an entry (`memory promote <id> --type ...`) only if it earns a place as a preference or fact.

## Mail

Mail is for coordination between sessions or agents, never for the canonical record:

- A parallel session needs a ruling from the map owner: `nitro agent mail send --to <owner> --actor maya --subject "[bill-3f2] Which export format?" --body "..."` with the question and the ticket id.
- Handoff to the orchestrator (see handoff.md): one briefing with task ids and ordering.
- Check `nitro agent mail inbox --unread --actor maya` at session start; answer with `mail reply` so threads stay intact, and record any ruling as a ticket comment.

The message is stored before the recipient's session is woken, so a non-zero exit means the wake went unconfirmed, not that the mail was lost; never resend on that alone.

## Task tickets

A `wayfinder:task` ticket (`--type task`) is manual work that blocks a decision: provision access, move data so its shape can be seen. Drive it alone where you can; otherwise hand the human a precise checklist in a comment and wait. Resolve with a comment recording what was done and the facts later tickets depend on (locations, URLs, row counts), then close. A task ticket that delivers a slice of the destination is mis-typed: close it and let the handoff cut it as an implementation task.

## Session hygiene

1. `nitro agent register --actor maya --role planner` (takes the role under the name your context states); `nitro agent mail inbox --unread --actor maya`; `nitro agent memory context --tag <memory tag>` -- this is where the effort's question style comes from; if it is missing, agree one before the first question and save it (grilling.md).
2. Load the map. Never edit it from memory of a previous session.
3. Claim, resolve, graduate, stop.
