# Research tickets: facts a decision waits on

A research ticket surfaces a fact from outside the working directory: vendor documentation, a third-party API, a standard, a knowledge base. It is AFK: a subagent resolves it while you keep working, and it is the one ticket type you may resolve several of in one session.

## When to create one

Create a research ticket when a grilling question would otherwise ask the user for something they would have to look up, and other tickets will depend on the answer. Make it a real ticket, not a side note: the `blocks` edge is what renders that dependency in the frontier. A fact only the current ticket needs is looked up inline by a subagent and recorded in that ticket's resolution comment instead.

Facts that come from the codebase itself are not research tickets; look them up in-session.

## Dispatching

Claim the ticket first (`update <id> --claim`), then dispatch the subagent. While charting, do this for every research ticket right after wiring edges, in parallel. While working the map, do it for every research ticket on the frontier at the start of the session, before the one decision you resolve by hand.

Give the subagent the ticket's question verbatim plus:

1. Investigate against **primary sources**: official docs, source code, specs, first-party APIs. Not a secondary write-up of them. Follow every claim back to the source that owns it.
2. Write the findings as a Markdown file with each claim cited. Lead with the answer to the question, then the evidence, then what remains unknown.
3. Save it where the repository already keeps such notes (`docs/research/` is a common choice); match the existing convention, and state the path in the report. If the repository wants research off the main branch, commit it in a separate worktree on a `research/<slug>` branch (see Git rules in operations.md) and report the branch.
4. Return the answer and the path or branch. Do not edit the tracker; the session that dispatched you records the resolution.

## Resolving

When the subagent reports:

```bash
nitro agent tasks comment add bill-3f2.2 --actor maya "$(cat <<'EOF'
## Decision
Exports land in the existing finance share; the reconciliation job already mounts it.

## Rejected
- New bucket: finance has no credentials for it and would need a second tool.

## Locked parameters
- path: /finance/exports/invoices/

## Assets
- docs/research/export-storage.md
EOF
)"
nitro agent tasks close bill-3f2.2 --actor maya --reason "Researched: finance share, path locked"
```

The comment must carry the answer, not only a pointer. A future session reads the ticket first and the file only if it needs the evidence. Then append the gist to the map's **Decisions so far** and run the graduation pass: a fact often turns a fog entry into a sharp question.

## Guardrails

- A subagent that could not reach a primary source reports that, and the ticket stays open with a comment saying what is missing. Never close on a guess.
- Research resolves facts, not decisions. If the finding forces a choice, the choice is a grilling ticket.
