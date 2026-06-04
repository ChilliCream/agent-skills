# Contributing skills

This repository hosts ChilliCream's official agent skills. All skills conform to the [Agent Skills open standard](https://agentskills.io/specification).

If you are writing a skill, prefer to do it from inside this repo with Claude Code — the local [`skill-author`](.claude/skills/skill-author/SKILL.md) skill auto-loads and contains the detailed authoring guide. The rules below are the short version.

## Directory layout

```
skills/
  <skill-name>/             # kebab-case; must match the `name:` frontmatter
    SKILL.md                # required
    scripts/                # optional: executables the agent may run
    references/             # optional: deeper docs loaded on demand
    assets/                 # optional: templates, schemas, images
```

## SKILL.md frontmatter

Required:

```yaml
---
name: my-skill              # 1–64 chars, lowercase a-z, digits, single hyphens
description: One or two sentences saying what the skill does AND when an agent should fire it. Include concrete triggers.
---
```

Optional: `license`, `compatibility`, `metadata`, `allowed-tools`. See the spec for details.

**The description is load-bearing.** Agents pre-load only the description to decide if a skill is relevant. Vague descriptions never fire. A good description names the capability and the triggers (libraries, file types, error messages, user phrases).

## Naming

- Folder name, `name:` field, and any references in `marketplace.json` must all match.
- Kebab-case everywhere: `hotchocolate-resolver-author`, not `HotChocolateResolverAuthor`.
- Prefix by product when natural: `hotchocolate-*`, `green-donut-*`, `mocha-*`, `bcp-*`.

## Body size

Keep `SKILL.md` under ~500 lines. Move anything the agent doesn't always need into `references/<TOPIC>.md` and point at it from the body. Smaller files mean the agent only pays for context it actually uses.

## Registering the skill

Add the skill path to `.claude-plugin/marketplace.json` under the appropriate plugin's `skills` array. The plugin manifest is what makes the skill installable via `/plugin install` in Claude Code.

## Pre-PR checklist

- [ ] Folder name matches `name:` in frontmatter.
- [ ] Description names at least one concrete trigger (*"Use when…"*).
- [ ] Body has at least one worked example.
- [ ] `SKILL.md` is under 500 lines (otherwise split into `references/`).
- [ ] No secrets, customer data, or internal-only URLs.
- [ ] Registered in `.claude-plugin/marketplace.json`.
- [ ] Verified by opening a fresh Claude Code session in a test repo and checking the skill fires on its intended triggers.

## Tone for skill bodies

- Imperative: *"Use a record for inputs."* Not *"You should consider records."*
- Explain *why* whenever the rule is non-obvious. Rules without reasons collapse on edge cases.
- No ALL-CAPS shouting. Reserve emphasis for actual landmines.
- Show wrong-vs-right examples in real ChilliCream code.

Full guidance with examples: [`.claude/skills/skill-author/SKILL.md`](.claude/skills/skill-author/SKILL.md).
