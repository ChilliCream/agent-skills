# Running the wave pipeline on Venti

How the harness-neutral roles and protocol in SKILL.md map onto Venti.

## Models and effort

The workflow declares roles with `Venti.Role` and proposes models and effort before execution. Override the proposals with this map, in either an all-Anthropic or an all-GPT stack:

| Role | Anthropic | GPT |
| --- | --- | --- |
| Implementer | claude-sonnet-5-5 high | gpt-6.1-terra high |
| Implementer, hard tickets | claude-opus-5-5 medium | gpt-6.2-sol medium |
| Reviewer | claude-opus-5-5 medium | gpt-6.2-sol medium |
| Reviewer, hard tickets | claude-fable-5-2 medium | gpt-6.2-astra medium |
| Verifier | claude-fable-5-2 high | gpt-6.2-astra high |
| Orchestrator session | claude-fable-5-2 medium | gpt-6.2-astra medium |
| Planner session | claude-fable-5-2 high | gpt-6.2-astra high |

- Set every effort explicitly. Venti defaults several GPT models to `low`, which makes them stop early.
- On hard tickets the reviewer moves up a tier, so the hard-ticket implementer's model never reviews its own work. A ticket is hard when it meets two or more of: crosses several projects or changes an interface with many implementers; its acceptance test needs layers outside the file scope; open design questions; concurrency or state machines; model-visible text or snapshot-heavy output; overlap with in-flight tickets.
- Role picks apply to a whole run, so put hard tickets in their own wave run.
- The fixer continues the implementer session at its original model and effort; there is no separate fixer role.
- The planner and the orchestrator are separate sessions, not roles in the workflow.

When lineups change, re-derive the map by tier instead of copying model names.

## The wave is a workflow

A wave runs as the Workflow script in [assets/workflows/nitro-backlog-wave.workflow.cs](../assets/workflows/nitro-backlog-wave.workflow.cs): deterministic implement, review, verify, fix loop, model and effort per role, typed results enforced. Load `workflow-authoring`, then per wave, after claiming tickets, pass the asset's absolute path:

```text
Workflow({ name: "Nitro Backlog Wave", path: "<absolute-skill-directory>/assets/workflows/nitro-backlog-wave.workflow.cs",
  args: { actor, repo, branch, area, rules?, tooling?,
    tickets: [{ id, mode: "implement", commits: [], priorCycles: 0, context: null }] } })
```

The first call compiles and proposes role picks; start it with `Workflow({ confirm: { run: "<returned-run-id>", roles: { ... } } })`, overriding the picks with the model map above. Confirm the picks yourself; do not ask the user to approve them. End the turn after confirmation; `WorkflowFinished` wakes the session. Do not poll.

When `WorkflowFinished` arrives, keep going in that turn: verify and close the passes, escalate the rest, drain mail, and launch the next wave. A wave summary is not a stopping point. End the turn only at the run-end condition in SKILL.md's Rhythm, or when every remaining ticket is blocked with the planner.

Each ticket keeps one implementer and one reviewer for its whole run, so neither has to rebuild context between cycles. The reviewer never edits files.

When a review fails, a fresh verifier checks every finding and writes the smallest correction plan. The implementer applies only that plan. A correction plan of more than 8 items is split into several writer turns of at most 8 items each, in dependency order. Each turn gets one "done when" line and is committed before the next starts. Never hand a writer the whole list in one turn. The reviewer then checks the correction, plan adherence, scope and regressions rather than reviewing unchanged code again. It reopens a dismissed finding only with new evidence; otherwise an intentionally dismissed suggestion can be mistaken for an unaddressed defect.

```text
Implementer A -> Reviewer B -> fresh Verifier C
Implementer A (applies plan) -> Reviewer B (checks correction) -> fresh Verifier D
Implementer A (applies plan) -> Reviewer B (checks correction) -> pass or escalate
```

If the verifier dismisses all blocker and major findings, the ticket passes without a correction. A third failed review goes to the planner, with no further verifier or correction.

Each run takes one area and at most twelve tickets, and handles tickets one at a time. Split larger waves into several runs.

To re-review existing commits, pass `mode: "review"` with `commits` and `priorCycles`. This starts a new reviewer, because earlier runs' conversations do not carry over. If corrections are needed, a new implementer starts from the verified plan.

The workflow reads each ticket with `nitro agent tasks show` itself and embeds it in every agent prompt; a ticket it cannot read ends as `ticket-unreadable`. The result carries per ticket the outcome, commits, cycles, and deferred notes: verify and close the passes with their commits as evidence, escalate the rest. Standing rules from memory go into `args.rules`.

Agents share the working directory: start in the target repository or a prepared worktree, pass its absolute path as `repo`, and run no other writers alongside the workflow. Nitro resolves the same agent workspace from every worktree of the repository, so no extra setup is needed.

## Wake integration

Install nitro's turn-boundary hooks so mail reaches sessions without polling:

```bash
nitro agent hooks venti install --scope user  # or --scope project
nitro agent hooks venti status               # add --scope project for project hooks
```

With hooks installed, Nitro gives the session an actor name and states it in your context. The session appears in `nitro agent list`, so mail addressed to it can wake it, and unread mail is shown to you at the start of each turn. Unread mail also blocks you from ending a turn, up to three times, before it lets you finish. When no actor name reaches the context, `nitro agent login` allocates one and you pass it as `--actor <name>` exactly the same way. Read hook command results from the human output and exit codes, not `--output json`.

Hooks or not, the rhythm stands: drain `nitro agent mail inbox --unread --actor <name>` between waves. The digest is a wake-up, not the inbox.
