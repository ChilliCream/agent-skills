# Running the wave pipeline on Venti

How the harness-neutral roles and protocol in SKILL.md map onto Venti.

## Models and effort

Apply the tier rule to Venti's available lineup; the workflow declares roles with `Venti.Role` and proposes models and effort before execution:

- **Planner**: usually a separate session running the nitro-task-planner skill, not a subagent you spawn.
- **Implementer**: Sonnet/Terra-class model at high effort.
- **Reviewer**: Opus/Sol-class model at medium effort.
- **Verifier**: Fable/Astra-class model at medium effort.
- **Fixer**: Sonnet/Terra-class model at high effort.

When lineups change, re-derive from the tier rule instead of copying model names; check and override the proposed picks before confirming.

## The wave is a workflow

A wave runs as the Workflow script in [assets/workflows/nitro-backlog-wave.workflow.cs](../assets/workflows/nitro-backlog-wave.workflow.cs): deterministic implement, review, verify, fix loop, model and effort per role, typed results enforced. Load `workflow-authoring`, then per wave, after claiming tickets, pass the asset's absolute path:

```text
Workflow({ name: "Nitro Backlog Wave", path: "<absolute-skill-directory>/assets/workflows/nitro-backlog-wave.workflow.cs",
  args: { actor, repo, branch, area, rules?, tooling?,
    tickets: [{ id, mode: "implement", commits: [], priorCycles: 0, context: null }] } })
```

The first call compiles and proposes role picks; start it with `Workflow({ confirm: { run: "<returned-run-id>" } })`. End the turn after confirmation; `WorkflowFinished` wakes the session. Do not poll.

Each phase is a fresh agent; the review result carries the findings forward. This workflow takes one area per call; tickets and waves run one at a time. Each ticket gets at most three review cycles; a third failed review escalates to the planner. With that default cap, pass at most five tickets per run. The result carries per ticket the outcome, commits, cycles, and deferred notes: verify and close the passes with their commits as evidence, escalate the rest. Standing rules from memory go into `args.rules`.

Agents share the working directory: start in the target repository or a prepared worktree, pass its absolute path as `repo`, and run no other writers alongside the workflow. Nitro resolves the same agent workspace from every worktree of the repository, so no extra setup is needed.

## Wake integration

Install nitro's turn-boundary hooks so mail reaches sessions without polling:

```bash
nitro agent hooks venti install --scope user  # or --scope project
nitro agent hooks venti status               # add --scope project for project hooks
```

With hooks installed, Nitro gives the session an actor name and states it in your context. The session appears in `nitro agent list`, so mail addressed to it can wake it, and unread mail is shown to you at the start of each turn. Unread mail also blocks you from ending a turn, up to three times, before it lets you finish. When no actor name reaches the context, `nitro agent login` allocates one and you pass it as `--actor <name>` exactly the same way. Read hook command results from the human output and exit codes, not `--output json`.

Hooks or not, the rhythm stands: drain `nitro agent mail inbox --unread --actor <name>` between waves. The digest is a wake-up, not the inbox.
