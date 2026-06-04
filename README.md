# ChilliCream Agent Skills

Agent skills for the ChilliCream ecosystem — HotChocolate, Banana Cake Pop, Green Donut, Mocha, and friends.

Skills follow the [Agent Skills open standard](https://agentskills.io/specification) and work with Claude Code, the Claude API, and any other compatible agent runtime.

## What lives here

Each skill is a folder under `skills/` containing a `SKILL.md`. The frontmatter says what the skill does and when an agent should fire it; the body teaches the agent how to perform the task — e.g. authoring a HotChocolate resolver, wiring a Green Donut DataLoader, extending Banana Cake Pop, or implementing a Mocha transport.

```
skills/
  <skill-name>/
    SKILL.md
    scripts/        # optional
    references/     # optional
    assets/         # optional
```

## Installing

### Claude Code (as a plugin)

```bash
/plugin marketplace add ChilliCream/agent-skills
/plugin install chillicream-skills@chillicream-agent-skills
```

### Anywhere else

Copy `skills/<name>/` into whatever directory your agent loads skills from. The format is portable across compliant clients.

## Authoring a new skill

The fastest path: open this folder in Claude Code and say *"I want to write a new skill for X."* The bundled local [`skill-author`](.claude/skills/skill-author/SKILL.md) skill will auto-load and walk you through the conventions.

Otherwise, read [AGENTS.md](./AGENTS.md) for the rules and the checklist.

## License

TBD.
