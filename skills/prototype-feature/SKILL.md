---
name: prototype-feature
description: >-
  Take a user's plain-English feature description and stand up a working UI
  prototype with realistic mock data — no GraphQL, no backend, just React +
  Tailwind + local state — that slots into the existing app design system
  and is hand-off-ready for the `mock-to-contract` skill later. Use when
  the user wants to mock up a new feature, prototype a page, wireframe a
  section, build a UI for something without backend yet, sketch out a new
  view, or explicitly says "do not write any GraphQL yet" / "let's just
  build the UI" / "no backend yet". Triggers on "prototype X", "mock up
  X", "build a UI for X", "wireframe X", "sketch out X", "add a new
  section for X", "let's just build the UI", "no graphql yet".
---

# Prototype a feature: mock UI first, contract later

This skill captures the rapid-prototyping phase that precedes
[mock-to-contract](../mock-to-contract/SKILL.md). The user describes a
feature in plain English; you stand up a working UI with realistic mock
data, wired into the existing app shell, in one pass. No GraphQL, no
fetching, no schema work — those come later.

The two skills are a sequence:

1. **prototype-feature** (this skill) — get the UI on screen with mock
   data so the user can see the shape of the thing
2. **mock-to-contract** — once the UI is approved, walk the tree, extract
   fragments, build the schema contract for the backend team

Use this skill the moment the user describes a feature and either says
"no backend yet" / "just the UI" or it is clear from context they want to
iterate visually before committing to a schema.

## The workflow

### 1. Restate the feature in one sentence

Before writing any code, restate what the user asked for in a single
sentence that names the actors, the entity, and the verbs. Confirm with
the user only if the description is ambiguous; otherwise proceed.

> "Users can post events in a planning area, other users can attend."

This sentence shapes everything that follows: the entity type
(`Event`), the actors (`User`/`Attendee`), the verbs (`post`, `attend`),
and the scoping (`area`).

### 2. Plan the data shape

Sketch a TypeScript type for the entity that mirrors the fields a future
GraphQL schema would expose. The shape choices you make here are
load-bearing — `mock-to-contract` walks this surface later, so:

- Use plausible field names — `startsAt`, not `when`; `attendees`, not
  `peopleGoing`
- Use plausible scalar types — `string` for dates that will become
  `DateTime`, numbers for counts
- Use enums for known value sets (categories, statuses), even as
  TypeScript string unions
- Include the fields the UI actually renders — no more, no less

Put this in `src/data/<feature>.ts` (e.g. `src/data/events.ts`).

### 3. Generate realistic mock data

The single most important quality bar in this skill: **mock data must
feel real**. Lorem ipsum and "Item 1, Item 2" placeholder names destroy
the prototype's usefulness — the user cannot evaluate the UI when the
data is meaningless.

Domain-appropriate content means:

- Names of real-ish places, people, products in the project's context.
  In a Singapore-themed app: "Maxwell Food Centre", "East Coast Park",
  "Tampines Hub", host name "Priya Naidu". Not "Location 1", "User A".
- Times and dates near the present (compute relative to `new Date()`)
- Numbers in plausible ranges (3-30 attendees, not 99999 or 1)
- Variety across the dataset — different categories, sizes, full vs
  empty capacity, with/without optional fields — so every visual state
  surfaces naturally

Aim for 5-8 mock entries. Fewer than that and edge cases hide; more than
that and the file becomes noise.

Also produce constants the UI needs but the backend will eventually own:
the current-user stub, the available category values, the list of areas
or any other reference data. Mark these clearly as mock so
`mock-to-contract` can find and replace them.

### 4. Enumerate the UI states

Before building any component, list the visual states the user will see.
At minimum:

- **Filled** — the happy path with multiple items rendered
- **Empty** — what shows when filters/search return nothing, or the
  feature is brand new ("nothing planned in {area} yet — be the first")
- **Per-item variants** — full vs not-full, viewer-attending vs not,
  with vs without optional fields (capacity, photo)

Add more states as the feature demands: create/edit modals, error states
for forms, loading skeletons for parts that will later be async.

If the user did not mention an empty state but the feature has filters
or search, build one anyway. Empty-state copy that nudges action
("Post the first event") is part of the design.

### 5. Find the integration point in the existing app

Read the app shell to learn where the new feature belongs. For an app
with a sidebar + view switcher, that usually means:

- A new entry in the `ViewId` union (or equivalent route enum)
- A new entry in the sidebar nav array with an appropriate icon
- A new case in the view-switching component (`App.tsx` switch)

For a tabbed app, that means a new tab. For a routed app, that means a
new route. The principle is the same: do not introduce a new navigation
pattern just for this feature. Slot into what exists.

### 6. Reuse the existing design system

Look for shared primitives the codebase already has — `Card`,
`StatTile`, `PageHeader`, `Field`, the Tailwind colour tokens, the icon
library. Use them. The prototype should be visually indistinguishable
from a feature shipped in the same release as everything else.

Concrete checklist:

- Same colour tokens (in this project: `brand-*`, `mint-*`, `sun-*`,
  `sky-*`, `slate-*`)
- Same spacing scale (the project's existing card/section paddings)
- Same icon family (here: `lucide-react`)
- Same shape vocabulary (rounded corners, ring borders, shadow scale)
- Same typography (font weight, size, tracking)

Do not introduce a new design token, font, or icon family for the
prototype. If the prototype needs something the design system lacks, ask
the user — that is a design-system decision, not a feature-build
decision.

### 7. Build bottom-up

Build the smallest, most reusable component first; compose upward.

1. Feature-specific primitives that depend on no other feature code
   (e.g. an `EventCategoryPill` that takes an enum prop)
2. Mid-level cards / list items (`EventCard`)
3. Modals and forms (`CreateEventDialog`)
4. The view component that composes everything (`EventsView`)
5. The app-shell wiring (sidebar nav, route enum, App switch)

Each component takes its data as props from the parent. No `useContext`,
no Zustand, no global store — keep state local to the view that owns
it. `useState` for everything mutable; `useMemo` for derived lists
(filtered/sorted).

### 8. Wire interactivity with local state

Mutations are local: clicking "Attend" updates a `Set<string>` of
attended IDs; submitting "Create event" prepends a new item to the
events array. This is intentional — the prototype must feel real to the
user without any of the backend infrastructure in place.

Make the interactions complete:

- Optimistic-looking updates: clicking Attend immediately shows "Going"
- Form validation lives in the component (basic length checks, required
  fields)
- The create form auto-fills the current context (e.g. default to the
  currently selected area)

When the future backend mutation is obvious (`attendEvent`,
`postEvent`), leave a `// TODO` comment naming it so `mock-to-contract`
can find and replace.

### 9. Verify it compiles and renders

Before declaring done:

- `npx tsc -b` — must exit 0
- `npm run build` — must succeed (catches anything tsc missed)
- The dev server (already running, usually at `localhost:5173`) must
  still serve a 200 on `/`
- Open the new view in your head: navigate the user's flow end-to-end
  (sidebar click → page renders → filter → search → empty state →
  create → see it in the list → attend)

If the dev server died on a config change, restart it. Hot-module reload
should pick up component changes automatically — no full restart needed
when only `.tsx` files change.

### 10. Hand off to `mock-to-contract` when the UI is approved

This skill ends here. When the user is happy with the prototype and
ready to wire it to a backend, switch to
[mock-to-contract](../mock-to-contract/SKILL.md). That skill walks the
tree you just built, splits where the prototype glossed over
componentization, and turns every mock value into a colocated Relay
fragment backed by a schema extension.

Hand-off readiness checklist:

- All mock data lives in identifiable mock files (`src/data/<feature>.ts`)
  with no scattered hardcoded values in components
- Field names in the mock types match what the schema fields should be
  called
- Each component's data needs are visible at its prop boundary (props
  carry the full entity, not a flattened bag of scalars)
- `// TODO` comments name the future mutations clearly

## Hard rules — non-negotiables

- **No GraphQL.** No `graphql\`\`` tags, no `useFragment`, no
  `usePreloadedQuery`, no schema extensions, no relay-compiler runs.
  This is the prototype phase. If the user asks for GraphQL at this
  stage, switch skills — they want `mock-to-contract` instead.
- **No `fetch`, `useEffect` data loading, or simulated latency.**
  Everything is synchronous local state. Async-looking states (loading
  skeletons) are visual exercises, not real awaits.
- **No new design tokens.** Reuse the existing Tailwind palette / icon
  library / typography scale. Introducing a new colour or font is a
  design-system PR, not a feature prototype.
- **No new state-management library.** `useState` and `useMemo` cover
  every prototype. Adding Zustand/Redux/Jotai for a prototype is over-
  engineering and complicates the later contract walk.
- **No features the user did not ask for.** If they said "users can post
  and attend", build those two verbs only. Do not add edit / delete /
  share / report. Each unsolicited feature widens the contract surface
  for no benefit.
- **Mock data must feel real.** Lorem ipsum, "Test 1 / Test 2", random
  uuids in names — all forbidden. The user cannot evaluate UX on
  meaningless data.
- **Empty state is not optional.** If the feature has filters, search,
  or starts empty for new users, the empty-state UI ships with the
  prototype.

## Worked example: the events feature

User prompt:

> "Can you add another sidebar item events. event are events that users
> can post in a region. users can also attent events. do not write any
> graphql stuff yet. let's just build the UI"

Walking the workflow:

1. **Restate:** "Users post events scoped to a planning area; other
   users can RSVP to attend."
2. **Data shape (`src/data/events.ts`):** `CityEvent` type with `id`,
   `title`, `description`, `category` (enum), `startsAt`, `durationMinutes`,
   `location`, `areaName`, `host`, `attendees`, `capacity`. Plus a
   `CURRENT_USER` constant and `AVAILABLE_AREAS` list.
3. **Mock data:** 7 Singapore-themed events — "Maxwell hawker crawl"
   hosted by Pascal Senn in City, "East Coast Park beach cleanup" in
   Bedok with 6 attendees, "GraphQL Singapore meetup" in City with 80
   capacity, "Tampines run club — 8km loop" with 3 RSVPs. Variety across
   categories, areas, capacities, attendee counts.
4. **UI states:** filled grid, empty-state card with "Be the first"
   CTA, search input, category filter chips, attend/going/full button
   states, create modal.
5. **Integration point:** new `events` value in `ViewId` union, new
   `CalendarHeart`-icon entry in Sidebar `NAV`, new case in `App.tsx`
   view switch.
6. **Design system reuse:** `PageHeader` for the title, three `StatTile`s
   for the stats row, the existing `card` Tailwind class for event
   cards, `lucide-react` icons throughout, brand/mint/sun/sky colour
   tokens for category pills.
7. **Bottom-up build:** `EventCategoryPill` → `EventCard` →
   `CreateEventDialog` → `EventsView` → app-shell wiring.
8. **Local state:** `useState<CityEvent[]>` for the events list,
   `useState<Set<string>>` for attendance, `useState` for dialog open
   and filter chips.
9. **Verify:** `npx tsc -b` exit 0, build succeeds, dev server still
   serves, click through the flow.
10. **Ready for `mock-to-contract`** — every mock value lives in
    `src/data/events.ts`, no scattered hardcoded data in components.

The prototype shipped and the user iterated on it ("remove the All
Singapore toggle — the page is per region") before any backend work
started — which is exactly what this skill is for.
