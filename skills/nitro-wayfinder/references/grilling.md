# Grilling: resolving a decision with the human

Grilling is how HITL tickets get resolved, and how the destination and the initial frontier get named while charting. Interview the user relentlessly, scoped to the one decision at hand, until you reach a shared understanding. Map the decision as a **design tree**: every answer branches into the decisions that hang off it.

## Rounds over the frontier

Work the tree in rounds. The frontier of a round is every question whose prerequisites are already settled: the questions you can ask now without guessing at answers you have not heard. Ask the whole frontier in one round, numbered, each with your recommended answer. Then wait.

A question whose answer depends on another question still open in this round belongs to a later round.

Format a round like so:

```
❓ **Q1** - **Export format**: Invoices are consumed by the finance team's spreadsheet import and by a nightly reconciliation job. Which format do we export? Options: CSV (RFC 4180), JSON lines, both.

➡️ CSV. Both consumers already parse CSV; JSON lines adds a second code path for no current consumer.

---

❓ **Q2** - **Encoding and line endings**: Finance opens exports in Excel on Windows. UTF-8 with BOM and CRLF, or plain UTF-8 with LF?

➡️ UTF-8 with BOM, CRLF. Excel misreads plain UTF-8 umlauts; the reconciliation job does not care.
```

Each answer reshapes the tree: settled decisions push the frontier outward and unblock what depended on them. Recompute the frontier and ask the next round.

## Rules

- **Every question is self-contained.** The context sits inside the question. Never "as discussed above" or a bare option list; a question that gets "what?" back lacked context, so rewrite it in plain prose and ask again.
- **Terse, plain language.** Short sentences. No jargon the user did not introduce.
- **Facts are your job, never the user's.** When a question needs a fact from the environment (what the schema looks like, what a library supports, what a vendor's API does), dispatch a subagent to find it. Do not block the round on it: only the questions downstream of that fact wait; ask the rest now. Feed the finding into the next round. A fact only this ticket needs goes into this ticket's resolution comment; a fact other tickets will depend on becomes a research ticket with a `blocks` edge (see research.md).
- **Decisions are the user's.** Put each to them and wait. Never fill in the user's side of the exchange, and never treat a recommendation as an answer.
- **If the user declines to answer a round**, re-ask the same round once when they say "ask again". If they answer with confusion instead of a choice, the question lacked context, not the user: rewrite it.
- **Record as you go.** A ruling given mid-round is a fact for the ticket; put it into the resolution comment, not only into the next question.

## Model the domain as you grill

Decisions are made in words, so sharpen them while you ask. Challenge terms that are vague or overloaded ("export" the file, or "export" the job?) and propose one canonical term. When the user states how something works, invent an edge case and test the statement against it. If the repository keeps a glossary or decision records (a `CONTEXT.md`, `docs/adr/`), read them before the first round and put resolved terms and decisions there in the same session, following the repository's convention; the ticket comment links to them.

## When the decision lands

The frontier is empty: every branch visited, nothing silently assumed. Restate the decision in one short paragraph and ask the user to confirm the shared understanding. On confirmation, immediately write the resolution comment (decision, rejected options and why, every locked parameter; template in operations.md), close the ticket, append to the map, and run the graduation pass. Do not act on the decision before the confirmation, and do not let further chat delay the write after it.

## Charting rounds

While charting, the first grill pins the destination (two lines, fixes scope). The second grill is breadth-first: fan out across the whole space rather than deep on any one thread, to surface the decisions that can be stated now and the fog that cannot. Depth belongs to the ticket sessions, not to charting.
