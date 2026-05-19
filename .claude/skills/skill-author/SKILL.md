---
name: skill-author
description: Authoring guide and conventions for ChilliCream agent skills. Use whenever the user wants to create a new skill, edit an existing one, improve a skill's description, or asks how to structure a SKILL.md file. Triggers on phrases like "write a skill", "new skill", "author a skill", "improve this skill", "skill description", "SKILL.md", or when editing any file under skills/*/SKILL.md or .claude/skills/*/SKILL.md.
---

# skill-author

This skill is the source of truth for how skills are written in the ChilliCream agent-skills repository. Read it (and follow it) whenever you are creating or editing a `SKILL.md`.

If you are starting a new skill from scratch, copy [`references/TEMPLATE.md`](references/TEMPLATE.md) into `skills/<your-skill>/SKILL.md` and fill it in.

## Mental model: progressive disclosure

Agent skills are loaded in three tiers. Design every skill around this hierarchy.

1. **Metadata** — `name` + `description`. Loaded into the agent's context at startup for every installed skill. ~100 tokens each. This is what makes the skill fire.
2. **Body** — the rest of `SKILL.md`. Loaded only after the agent decides the skill is relevant. Keep under 500 lines.
3. **References / scripts / assets** — loaded on demand by the agent or invoked when needed.

If you put trigger information only in the body, the skill will never fire — the agent doesn't see the body until *after* it decides to load the skill. Put triggers in the description.

## File layout

```
skills/<skill-name>/
  SKILL.md              # required
  scripts/              # optional executables the agent may run
  references/           # optional deeper docs the agent reads on demand
  assets/               # optional templates, schemas, images
```

- Folder name and the `name:` field must match exactly.
- Lowercase kebab-case throughout.

## Frontmatter

```yaml
---
name: hotchocolate-resolver-author
description: Write idiomatic HotChocolate v15 resolvers, mutations, and DataLoaders in graphql-platform repositories. Use when adding or editing a class with [QueryType], [MutationType], extending ObjectType<T>, when the user mentions HotChocolate, Banana Cake Pop, or GraphQL schema design in a .NET project, or when working in any repo under graphql-platform/.
---
```

Field rules (from the [spec](https://agentskills.io/specification)):

| Field | Required | Rule |
|---|---|---|
| `name` | yes | 1–64 chars, lowercase `a-z`/digits/single hyphens, no leading/trailing/double hyphens, matches folder. |
| `description` | yes | 1–1024 chars. Capability **and** trigger. |
| `license` | no | Short license name or pointer. |
| `compatibility` | no | Only if the skill needs a specific runtime or tool. |
| `metadata` | no | Free-form string-to-string map. Use unique keys. |
| `allowed-tools` | no | Experimental. Space-separated pre-approved tools. |

## Writing the description (the most important step)

The description has two jobs: tell the agent **what the skill does** and **when to fire it**. Both matter equally.

Good descriptions:
- Lead with the capability in one clause.
- Name explicit triggers — file types, attribute names, library names, error messages, user phrases.
- Are slightly pushy. Agents tend to *under*-trigger skills, so bias toward firing.

**Bad** — vague, no triggers:

```yaml
description: Helps with HotChocolate.
```

The agent has no idea when "helping with HotChocolate" is the right move. Skill never fires.

**Good** — capability + triggers:

```yaml
description: Write idiomatic HotChocolate v15 resolvers, mutations, and DataLoaders. Fire whenever the user adds or edits a class with [QueryType], [MutationType], or extends ObjectType<T>, mentions HotChocolate or Banana Cake Pop, or asks about GraphQL schema design in a .NET project.
```

If your skill is scoped to specific repos or products, name them: *"in repositories under graphql-platform/"*, *"when working with .NET 9 projects"*.

If you need to choose between describing the skill at length and listing more triggers, list more triggers.

## Writing the body

The body is plain Markdown. There is no required structure, but the following outline earns its keep most of the time:

```markdown
# <skill name>

One paragraph framing the task. What outcome is the agent trying to produce?

## When to use

Optional. Only if the trigger nuance can't fit in the description.

## Instructions

Imperative, ordered steps. The actual procedure.

## Examples

At least one worked input → output pair. Real ChilliCream code beats abstractions.

## Gotchas

Edge cases, common wrong patterns, things the agent will get wrong without explicit guidance.

## References

Pointers into `references/` for deeper material that doesn't need to live in the main body.
```

## Tone and style

- **Imperative.** *"Use a record for inputs."* Not *"You should consider records."*
- **Explain why** whenever the rule is non-obvious. A rule with a reason survives edge cases the author didn't anticipate; a rule without one collapses on the first surprise. Tell the agent *why* records are preferred (immutability, value equality, init-only) and it'll handle the case you didn't think of.
- **Don't shout.** ALL-CAPS MUSTs read as panic, not authority. Save emphasis for actual landmines (data loss, security).
- **Generalize.** State the principle, then show examples. If you only write rules for the three cases you tested, the agent will fail on the fourth.
- **Trim ruthlessly.** Every line costs context. If a sentence doesn't change agent behavior, delete it.
- **Show wrong-vs-right.** A wrong example next to the right one teaches more than three paragraphs of rules.

## Examples beat abstractions

Bad — abstract:

> Resolvers should use proper dependency injection patterns.

Good — concrete, with code:

````markdown
**Example — resolver with a DataLoader:**

```csharp
public sealed class BookResolvers
{
    public async Task<Author?> GetAuthor(
        [Parent] Book book,
        AuthorByIdDataLoader authorById,
        CancellationToken ct)
        => await authorById.LoadAsync(book.AuthorId, ct);
}
```

Note: inject the DataLoader as a parameter, not via constructor — HotChocolate scopes the DataLoader to the request, and constructor injection breaks that.
````

## When to split into `references/`

Once `SKILL.md` exceeds ~500 lines, move detail into `references/`:

```
hotchocolate-resolver-author/
  SKILL.md
  references/
    DATALOADER.md
    SUBSCRIPTIONS.md
    ERROR-HANDLING.md
```

Then point at them from the body:

```markdown
For DataLoader patterns see [DataLoader reference](references/DATALOADER.md).
For subscriptions see [Subscriptions reference](references/SUBSCRIPTIONS.md).
```

The agent reads `references/*.md` only when the current task requires it. Smaller, focused references = less context burned on irrelevant content.

## Scripts

Use `scripts/` when the skill needs deterministic, repeatable execution that's easier to *run* than to *describe* — codegen, scaffolding, schema diffing.

Conventions:
- `#!/bin/bash` and `set -euo pipefail` for shell scripts.
- Status messages to stderr (`echo "..." >&2`), structured output (JSON) to stdout.
- Idempotent. Re-running must not corrupt state.
- Document the script's contract at the top of the file.

Reference scripts from the body with their relative path: `Run scripts/scaffold-resolver.sh <name>`.

## Anti-patterns

- **Description that only describes.** *"A skill for writing GraphQL resolvers."* Misses the "when" — the agent doesn't know which user requests should trigger it.
- **Body that re-states the description.** The body should add information the description couldn't fit, not repeat it.
- **One enormous `SKILL.md`.** Anything the agent doesn't always need belongs in `references/`.
- **No examples.** Rules without examples are advisory; the agent will improvise. Examples are the contract.
- **Untestable rules.** *"Write idiomatic code."* What does the agent verify? Replace with concrete rules + counter-examples.
- **Stale references.** Pointing the agent at files that have since been renamed or deleted. Re-check paths when editing skills.
- **Description bloat.** If you find yourself writing four sentences of triggers, the skill is probably two skills. Split it.

## Pre-publish checklist

- [ ] Folder name matches `name:` exactly.
- [ ] Description names a concrete capability and at least one trigger.
- [ ] Body has at least one worked example.
- [ ] `SKILL.md` is under 500 lines.
- [ ] References (if any) are linked from the body.
- [ ] No secrets, customer data, or internal-only URLs.
- [ ] Listed in `.claude-plugin/marketplace.json`.
- [ ] Tried the skill in a fresh Claude Code session and confirmed it fires on intended triggers.

## See also

- [`references/TEMPLATE.md`](references/TEMPLATE.md) — copy this when starting a new skill.
- [Agent Skills specification](https://agentskills.io/specification)
- [Anthropic skill-creator](https://github.com/anthropics/skills/tree/main/skills/skill-creator) — a more general, non-ChilliCream version of this guidance.
