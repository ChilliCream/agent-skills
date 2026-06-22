# ChilliCream Agent Skills

Agent skills for the ChilliCream ecosystem - HotChocolate, Nitro, Green Donut, Mocha, and friends.

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

## Available skills

### [`prototype-feature`](skills/prototype-feature/SKILL.md)

Build a clickable local-only frontend prototype before backend or schema work exists. The skill keeps the prototype aligned with the existing app, uses realistic product-domain mock data, preserves one-entity component boundaries, and models user actions as mutation-shaped local hooks.

Fire it with phrases like _"prototype this feature"_, _"mock up this page"_, _"wireframe this view"_, _"just build the UI"_, _"no backend yet"_, or _"no GraphQL yet"_.

### [`prototype-to-contract`](skills/prototype-to-contract/SKILL.md)

Convert a local-only prototype into GraphQL-client-backed component fragments and a backend contract. The skill replaces rendered mock fields with colocated fragments, turns local action hooks into schema mutations, preserves data-masked component boundaries, and writes schema contract SDL for the backend team.

Fire it with phrases like _"prototype to contract"_, _"mock to contract"_, _"turn this prototype into a contract"_, _"fragmentize this page"_, _"wire this mocked page to GraphQL"_, or _"spec this feature for the backend"_.

### [`graphql-schema-design`](skills/graphql-schema-design/SKILL.md)

Design and review GraphQL schema changes. The skill acts as a senior API architect that produces SDL proposals and design feedback — it does not write implementation code. It runs in two modes:

- **Design mode** (default) — given a feature, use case, or need, it walks through an iterative process to produce a thoroughly vetted SDL proposal you explicitly approve, covering types, mutations, queries, connections, enums, and error handling.
- **Review mode** — given a schema diff, it audits the changes for naming, nullability, evolvability, and client-friendliness.

The rules are framework-agnostic GraphQL design conventions, illustrated with a Book/Author domain. Fire it with phrases like _"design a mutation"_, _"new type"_, _"review schema diff"_, or `/graphql-schema-design`.

## Installing

### Claude Code (as a plugin)

```bash
/plugin marketplace add ChilliCream/agent-skills
/plugin install chillicream-skills@chillicream-agent-skills
```

### `skills` CLI (via npx)

```bash
npx skills add ChilliCream/agent-skills
```

This pulls the skills into your agent's skills directory. Works with any [Agent Skills](https://agentskills.io/specification)-compatible runtime.

### Anywhere else

Copy `skills/<name>/` into whatever directory your agent loads skills from. The format is portable across compliant clients.

## Authoring a new skill

The fastest path: open this folder in Claude Code and say _"I want to write a new skill for X."_ The bundled local [`skill-author`](.claude/skills/skill-author/SKILL.md) skill will auto-load and walk you through the conventions.

Otherwise, read [AGENTS.md](./AGENTS.md) for the rules and the checklist.
