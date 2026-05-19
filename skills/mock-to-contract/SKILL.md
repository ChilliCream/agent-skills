---
name: mock-to-contract
description: >-
  Turn a mock-data React+Relay feature into properly componentized code
  backed by colocated GraphQL fragments and a clean schema contract for the
  backend team. Use when the user wants to convert a mocked UI into a backend
  contract, walk the component hierarchy of a feature, replace mock/hardcoded
  data with fragments, componentize a page for Relay, fragmentize a feature,
  spec a feature for the backend team, or build a schema contract from the UI.
  Triggers on "mock to contract", "turn this mock into a contract", "walk the
  component hierarchy", "componentize this page", "fragmentize", "spec this
  feature for the backend", or "schema-ify this feature".
---

# Mock to contract: walk a feature, fragmentize, hand the schema to the backend

This skill takes a mocked-up feature — a React page with hardcoded data and
inline JSX — and turns it into the canonical Relay-best-practices shape:
small components, each declaring its own colocated GraphQL fragment, all
spreading into a single preloaded query at the route boundary, all backed by
additions written directly into `schema.graphql` that double as the contract
handed to the backend team.

Use this skill the moment a user mocks up a new page or section and is ready
to wire it for real. Do not invoke for one-off fragment additions to an
existing well-structured component — use [relay-best-practices](../relay-best-practices/SKILL.md) directly for that.

## Companion skills

This skill orchestrates two others; read them once at the start of the walk
and refer back as needed.

- **[relay-best-practices](../relay-best-practices/SKILL.md)** — the rules
  every fragment, query, and mutation in this walk must follow (inline
  graphql tags, render-as-you-fetch, never copy Relay data into useState,
  spread fragments into mutation responses).
- **[graphql-schema-design](../graphql-schema-design/SKILL.md)** — invoked in
  Review mode as the final step against the schema-extension file the walk
  produced.

## The walk

The pattern is depth-first, leaves-first, with two distinct passes per
component: an **entry** pass on the way down, and an **exit** pass on the way
up. Do not skip either pass.

```
walk(root)
  enter(root)          # split-check (going down)
  for each child of root:
    walk(child)        # recurse before doing exit work
  exit(root)           # mock-data → fragment (going up)
```

You only ever decide things about a component once you have seen all of its
descendants. Otherwise a split you make at the root can render the subsequent
work moot.

### Entry pass — when you enter a component

Ask: **is this component doing one thing, or is it doing several things
glued together?**

A component should split if any of the following is true:

- It renders an obvious sub-unit of UI that owns its own data slice
  (a card footer with avatars + a CTA button is two units, not one)
- It contains a data table or list that is bigger than the rest of the
  component combined
- It mixes a presentational shell with data-bound content (the shell
  becomes shared UI; the data-bound part becomes its own widget)
- Two different fragments would naturally live on different parts of the
  tree if the component were split

If it should split: extract the sub-component into its own file under the
same folder, **then recurse into it**. The new component is now part of the
walk. The original component composes it as a child.

If it should not split, move on — recurse into existing children.

Things that are not a reason to split:

- Pure layout helpers used once (a 5-line `Row` flex helper inside one
  card). Keep them as inline local functions.
- Repeated 1-2 line presentational fragments — extract only when the
  repetition crosses three sites.

### Exit pass — before you leave a component

Ask: **what hardcoded values would have to come from the backend in a real
implementation?**

A value is "mock data" if it would require a backend round-trip in production:

- An `INITIAL_EVENTS` array, a `CURRENT_USER` constant, a list of areas
- Counts derived from local arrays (`events.length`, the user's RSVP count)
- Image URLs, placeholder names, hardcoded titles
- Anything currently sourced from a `data/` or `mock*` file

A value is **not** mock data if it is intrinsic to the component:

- Tailwind class names, icon choices, formatting strings
- Local UI state (search query, dialog open, active filter)
- Pure-presentation maps (category → tone+icon for an enum prop)

For every mock value the component reads, introduce a colocated Relay
fragment that fetches the corresponding field. Follow
[relay-best-practices](../relay-best-practices/SKILL.md) verbatim — the
fragment tag must be inlined inside the `useFragment(...)` call, never
hoisted to a module-level const.

#### Resolving the right type for the fragment

1. **Look in the existing server schema first.** Read `schema.graphql` (or
   whatever the project's `relay.config.json` `schema` points at). Try to
   find a type that semantically matches the entity the component renders.
2. If you find one, use it directly.
3. If you find a partial match — the type exists but does not yet expose the
   field you need — add the field directly to the type in `schema.graphql`.
4. If no matching type exists, add the new type directly to `schema.graphql`.

#### Field and type quality bar

Every type, field, enum value, input, and payload **must** carry a
description that explains both purpose and business behavior. The schema
extension is the contract you hand to the backend team — a description like
`"""The user's name"""` is useless; the team needs to know *what the field
represents in the product*, *how the UI uses it*, and *what the null/error
semantics are*.

```graphql
# Bad — restates the field name
"""The location."""
location: String!

# Good — names the source, the UI usage, and the edge case
"""
Free-text meeting location (e.g. "Maxwell Food Centre, Stall 10",
"ECP Area C carpark"). Not necessarily geocoded — this is what the host
typed. Rendered as the "location" row on the event card.
"""
location: String!
```

Use SDL descriptions (`"""triple-quoted"""`), not `# comments`. Only
docstrings survive introspection and end up in the backend team's tooling.

#### Type naming when introducing

Follow the conventions in [graphql-schema-design/references/naming.md](../graphql-schema-design/references/naming.md):
PascalCase types, camelCase fields, SCREAMING_SNAKE enums, `Input`/`Payload`
suffixes for mutation types, unique-per-context connection types
(`AreaEventConnection`, not a shared `EventConnection`).

#### Connections, not lists

Any list that could grow unbounded over the lifetime of the API must be a
Relay connection, not a `[T!]!`. This is non-negotiable per
[graphql-schema-design/references/connections.md](../graphql-schema-design/references/connections.md).
Counts that go alongside the list (`upcomingEventCount`, `attendingCount`,
`totalRsvps`) belong on the connection as `totalCount`, not as separate
scalar fields.

#### Mutations: spec the contract

If the component triggers a write (an RSVP button, a "post" form), spec the
mutation in the schema extension even if you cannot wire `useMutation` yet
(client-extension Mutation roots are not supported by the current Relay
compiler). The contract must include:

- Unique `Input` and `Payload` per mutation
- Stage 6a errors: `event: Event` (nullable) + `errors: [<Mutation>Error!]`
- An `Error` interface + per-mutation error union + concrete error types
  with case-specific fields (`EventAtCapacityError { capacity: Int! }`)
- Symmetric counterparts (`postEvent` ⇒ also `cancelEvent`,
  `attendEvent` ⇒ also `leaveEvent`)

See [graphql-schema-design/references/errors.md](../graphql-schema-design/references/errors.md)
and [mutations.md](../graphql-schema-design/references/mutations.md).

Add the mutation, its Input, Payload, and error types directly to
`schema.graphql`. When the component's `useMutation` cannot be wired yet
(e.g. no resolver behind it), leave the onClick handler as a `// TODO` that
names the mutation it will call. Do not invent local state to fake mutation
behavior.

### After the walk

1. Update the route-level component to declare a `usePreloadedQuery` that
   spreads every child fragment in the page.
2. Wire `useQueryLoader` at the app shell so the query starts loading
   before the route renders.
3. Run `npm run relay` (or the project's equivalent script — check
   `package.json`). Fix every error before moving on.
4. Run `npx tsc -b`. Fix every error.
5. Delete the now-unused mock data files (`src/data/<feature>.ts`,
   `mockData.ts`, etc.). Anything that was only a stand-in for the backend
   must be gone before the contract is handed off.

### Mandatory review step

After the walk completes and compilation is green, **invoke the
[graphql-schema-design](../graphql-schema-design/SKILL.md) skill in Review
mode** against `schema.graphql`:

> /graphql-schema-design review @schema.graphql

Treat the resulting findings as gate items:

- **Issues** (must fix): apply the recommended changes, re-run the compiler
  and tsc, then come back for re-review of the changed area.
- **Warnings** (acknowledge): present each one to the user with the
  trade-off explicit. If the user accepts the trade-off, document the
  decision in `schema.graphql` as a `# Decision: …` comment on the affected
  type. Do not silently dismiss warnings.
- **Good**: leave alone — the review confirms patterns to keep.

Iterate `walk → review → apply → re-review` until the only remaining items
are warnings the user has explicitly accepted.

## Hard rules — non-negotiables

- **Walk leaves-first, both passes.** A top-down pass produces churn
  because splits at the root reshape children you already touched.
- **One fragment per component, declared inline at the `useFragment` call
  site.** Module-level `const FooFragment = graphql\`…\`` is forbidden.
- **Sibling fragments selecting the same field must use the same arguments.**
  Otherwise the parent query has a conflict. If two consumers genuinely
  need different args, alias one of them.
- **Every new schema element gets a description.** No exceptions, no
  `# comments` as a substitute.
- **Mock data files are deleted, not left as fallbacks.** If the
  component compiled before with mocks and after with fragments, the mock
  source must be unreferenced. Run `grep -r mockData src/` to verify.
- **Run the compiler centrally, never in parallel agents.** Concurrent
  `relay-compiler` runs race on artifact files. If you delegate widget
  conversion to parallel agents, run the compiler yourself after they
  finish — never inside the agent prompts.
- **The review step is not optional.** Even if the walk feels clean, the
  schema-design skill catches connection-vs-list mistakes, missing Stage 6a
  errors, generic naming traps, and unbounded lists every time.

## Walking an example feature

A typical events page tree:

```
EventsView                    (route — preloaded query)
├── PageHeader                (shared, no data)
├── StatTile × 3              (shared, no data)
├── filter bar                (inline — local UI state)
├── EventCard × N             (per-event fragment)
│   ├── EventCategoryPill     (enum prop, no fragment)
│   ├── Row (helper)          (inline, no extraction)
│   ├── EventAttendeesPreview (split out — fragment on Event)
│   └── AttendButton          (split out — fragment + mutation)
└── CreateEventDialog         (fragment on Query for areas; mutation for post)
```

Walking it:

1. `EventCategoryPill` enter→exit: presentational, no split, no fragment.
2. `Row` enter→exit: layout helper, leave inline.
3. Inside `EventCard`, identify two split candidates:
   `EventAttendeesPreview` (avatar stack + host text) and `AttendButton`
   (RSVP toggle). Extract both as siblings, then recurse into them.
4. `EventAttendeesPreview` exit: needs `host { name }` and an attendee list
   → declare `EventAttendeesPreview_event` on `Event` selecting
   `attendees(first: 4) { nodes { id name initials } totalCount }`.
5. `AttendButton` exit: needs `id`, `viewerIsAttending`, `capacity`, and the
   attendee count → declare `AttendButton_event` on `Event` selecting the
   same `attendees(first: 4) { totalCount }` so the two sibling fragments
   merge.
6. `EventCard` exit: declares its own fragment (`title`, `description`,
   `category`, `startsAt`, `durationMinutes`, `location`, `area { name }`,
   `attendees(first: 4) { totalCount }`) and spreads
   `...EventAttendeesPreview_event` and `...AttendButton_event`.
7. `CreateEventDialog` exit: drops the hardcoded area list, declares
   `CreateEventDialog_query` on `Query` selecting `areas { name }`.
8. `EventsView` exit: declares `EventsViewQuery($name: String!)` that
   selects `areaByName(name: $name) { name events(first: 50) { totalCount
   nodes { id ... ...EventCard_event } } }` and `viewer { ... }`, plus
   `...CreateEventDialog_query`. Wires `useQueryLoader` in App.
9. Compile + tsc clean.
10. **Review:** run `/graphql-schema-design review @schema.graphql`.
    Apply issues (unbounded lists → connections, Stage 6a on mutations,
    symmetric mutations, drop probe fields). Reconcile warnings with the
    user. Re-compile, done.
