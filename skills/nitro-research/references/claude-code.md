# Running research on Claude Code

How the harness-neutral roles in SKILL.md map onto Claude Code.

## Models and effort

The tier rule: locating and extracting get the cheapest model that quotes accurately; judging gets a mid tier; only adversarial verification gets a top tier, and only for investigations. As of 2026 a good mapping:

- **Lead**: whatever the session runs; it reads three short files and writes one paragraph, so its cost is the brief, not the model.
- **Scout**: sonnet, low effort
- **Reader**: sonnet, low effort; rerun a failed reader as `research-reader-retry` (sonnet, medium effort), then with an opus `model` override.
- **Synthesizer**: sonnet, medium effort
- **Verifier**: opus, medium effort; fable only when the caller says the decision is expensive to get wrong.

When lineups change, re-derive from the tier rule instead of copying these names.

Haiku is not in the mapping on purpose: it costs half of Sonnet on the cheapest roles, is a generation older, has a 200K context that dense sources overflow, and a reader that quotes the wrong location costs more in reruns than it saved. Lower effort on the newer model is the cheaper lever.

## Effort only works through agent definitions

The Agent tool takes a `model` override per call but no effort field. Effort comes only from a subagent definition (`.claude/agents/<name>.md`, frontmatter keys `model` and `effort`, values `low`, `medium`, `high`, `xhigh`, `max`). Writing "use low effort" in a prompt does nothing; the subagent runs at the harness default.

So the skill ships the roles as definitions in [assets/agents/](../assets/agents/). Before the first dispatch of a job, install them into the project:

```bash
mkdir -p .claude/agents
for f in <skill-dir>/assets/agents/research-*.md; do
  [ -e ".claude/agents/$(basename "$f")" ] || cp "$f" .claude/agents/
done
```

Existing files are kept, so a project can tune model or effort locally. Then spawn by name: `subagent_type: "research-scout"`, `"research-reader"`, `"research-reader-retry"`, `"research-synthesizer"`, `"research-verifier"`. Pass a `model` override only when escalating a failed reader; the definition's effort still applies. If the definitions cannot be installed (no write access to `.claude/`), spawn `general-purpose` with a `model` override and say in the close report that effort was not controlled.

## Spawning

Launch a fresh subagent for every role invocation by its definition name. Never fork: a fork inherits the lead's whole context, which is exactly the cost this pipeline avoids.

Every prompt is self-contained: name the role, give the absolute folder path and the file the agent must write, paste the role's contract from [briefs.md](briefs.md), and end with "Return at most ten lines: what you wrote, where, and any failure." Readers get one source each and run in parallel in a single message. Do not give subagents your actor name or any `nitro` command; the lead claims, comments, and closes every step task itself from the subagent's returned summary. Nothing from the lead's conversation reaches a subagent unless it is in the prompt or in the folder.

The definitions already grant the tools each role needs: scouts get WebSearch and WebFetch, readers WebFetch plus file access, synthesizer file access only, verifier file access plus WebFetch.

## Worktrees

When research must stay off the main branch, give the lead a worktree on `research/<slug>` (Agent tool `isolation: "worktree"` for the readers is unnecessary; they only write into the research folder). Commit the folder once at close.
