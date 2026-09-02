# Running the wave pipeline on Claude Code

How the harness-neutral roles and protocol in SKILL.md map onto Claude Code.

## Models and effort

Apply the tier rule to the current Claude lineup. As of 2026 a good mapping:

- **Planner (fable, medium effort)**: usually a separate session running the nitro-task-planner skill, not a subagent you spawn.
- **Implementer (sonnet, high effort)**
- **Reviewer (opus, medium effort)**
- **Verifier (fable, medium effort)**
- **Fixer (sonnet, high effort)**

When lineups change, re-derive from the tier rule instead of copying these names.

## Effort only works through agent definitions

The Agent tool takes a `model` override per call but no effort field. Effort comes only from a subagent definition (`.claude/agents/<name>.md`, frontmatter keys `model` and `effort`, values `low`, `medium`, `high`, `xhigh`, `max`). Writing "use high effort" in a prompt does nothing; the subagent runs at the harness default.

So the skill ships the four spawned roles as definitions in [assets/agents/](../assets/agents/). Before the first wave, install them into the project:

```bash
mkdir -p .claude/agents
for f in <skill-dir>/assets/agents/wave-*.md; do
  [ -e ".claude/agents/$(basename "$f")" ] || cp "$f" .claude/agents/
done
```

Existing files are kept, so a project can tune model, effort, or tools locally. Then spawn by name: `subagent_type: "wave-implementer"`, `"wave-reviewer"`, `"wave-verifier"`, `"wave-fixer"`. Do not pass a `model` override; the definition carries the tier. If the definitions cannot be installed (no write access to `.claude/`), spawn `general-purpose` with a `model` override and note in the final report that effort was not controlled.

## Spawning subagents

Spawn one subagent per role per ticket by its definition name: an implementer and then a separate reviewer for each ticket, plus a verifier and fixer only when review fails; resume the same agents for later phases with SendMessage and release them when the ticket closes. The definitions carry the role identity and the standing git rules; the prompt still has to be self-contained for the work: provide the ticket id and instruct the agent to read it with `nitro agent tasks show <id> --output json` (comments included); state the wave boundary ("touch only `<area>`; other waves are active elsewhere"); provide the implementation commit hashes or exact diff when applicable; restate any standing rules from memory the role must obey; and paste the applicable [change](../assets/change-result.schema.json), [review](../assets/review-result.schema.json), or [verification](../assets/verification-result.schema.json) result schema. Never rely on conversation context reaching the subagent—the prompt, the definition, and the tracker are its contract.

Worktree isolation is native: give each implementer its own worktree when waves run concurrently. Nitro resolves the same agent workspace from every worktree of the repository, so no extra setup is needed.

## Wake integration

Install nitro's turn-boundary hooks so mail reaches sessions without polling:

```bash
nitro agent hooks claude install --scope user  # or --scope project
nitro agent hooks claude status
```

With hooks installed, Nitro gives the session an actor name and states it in your context. The session appears in `nitro agent list`, so mail addressed to it can wake it, and unread mail is shown to you at the start of each turn. Unread mail also blocks you from ending a turn, up to three times, before it lets you finish. When no actor name reaches the context, `nitro agent login` allocates one and you pass it as `--actor <name>` exactly the same way. Read hook command results from the human output and exit codes, not `--output json`.

Hooks or not, the rhythm stands: drain `nitro agent mail inbox --unread --actor <name>` between waves. The digest is a wake-up, not the inbox.
