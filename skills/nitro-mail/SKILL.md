---
name: nitro-mail
description: >-
  Send and receive mail between coding agents with `nitro agent mail`:
  local-first, per-workspace messaging for multi-agent coordination. Use when
  registering an agent identity, messaging or replying to another agent,
  draining an inbox, or waiting for new mail.
---

# nitro agent mail

Mail is how agents in one workspace coordinate: registered identities, messages, threads. It shares the workspace with `nitro agent tasks` and `nitro agent memory`, lives locally with the repository across all its git worktrees, and never reaches the remote. Always call a subcommand; bare `nitro agent` and `nitro agent mail board` open interactive TUIs and block the session. If commands report no workspace, run `nitro agent init` once from the repository root (see the sibling `nitro-task` skill).

## Core principles

- **Mail carries pointers, never the canonical record.** Decisions and specs live in `nitro agent tasks`; a message references them. Correlate a thread to a task through the subject: `[app-1a2] Starting`.
- **You are your actor name.** Resolution per command: `--actor` > `NITRO_MAIL_ACTOR` > `NITRO_TASK_ACTOR` > OS user name, lowercased; only lowercase letters, digits, `-`, `_` are valid, anything else is rejected. Shell state does not persist between tool calls, so pass `--actor <name>` on every command.
- **Register at session start, verify with `whoami`.** `nitro agent register --actor <name> --role <role>` is a silent upsert: it never rejects a taken name, and omitting `--role` on a re-register clears the role -- always repeat it. `nitro agent whoami --actor <name>` confirms registration; `nitro agent list` shows only live harness sessions, so an idle registered agent may not appear there.
- **Read state is yours alone.** Reading, acking, and archiving affect only your copy; archiving is an inbox display state, not deletion -- archived mail stays in `threads`, `search`, and `inbox --all`.

## Send and reply

```bash
nitro agent mail send "agent-a" --actor alice --subject "[app-1a2] Starting" --body "Claiming this now."
nitro agent mail send "agent-a" "agent-b" --cc "agent-c" --actor alice --subject "Status" --body-file notes.txt
nitro agent mail reply "m-abc123" --actor alice --body "On it."
nitro agent mail broadcast --actor alice --role "backend" --subject "Heads up" --body "Deploying at 5pm."
```

- Sending to a never-registered name succeeds and creates an implicit mailbox for it, with a `note: '<name>' has never registered.`; only an invalid name is a hard failure. Check the spelling when the note surprises you.
- `reply` computes recipients from the thread: the original sender plus its to/cc, minus you, all flattened into `to`. Subject is inherited. Replying to a message you neither sent nor received fails.
- `broadcast` reaches every registered agent except you (implicit ones excluded); `--role` narrows it to that role's agents.
- Sends fire a best-effort wake ping at recipients with a live claimed session -- a convenience, never a delivery guarantee; add `--no-ping` for low-priority notes.

## Receive

```bash
nitro agent mail inbox --unread --actor alice --output json
nitro agent mail read "m-abc123" --actor alice --thread      # whole thread oldest first, marks it read
nitro agent mail ack "m-abc123" "m-def456" --actor alice     # mark read without printing
nitro agent mail archive "m-abc123" --actor alice            # done with it
nitro agent mail threads --actor alice --output json         # your threads, last activity first
nitro agent mail search "deploy" --actor alice --output json # subject, body, and sender, case-insensitive
```

- Ack what you act on, archive what is done, so other agents see the thread was handled.
- `ack` and `archive` batches are all-or-nothing: one unknown or foreign message id fails the whole batch. Archiving a message you only sent fails -- you have no recipient copy.
- `search` covers only mail you sent or received; it never surfaces another agent's private threads.

## Wait for mail

```bash
nitro agent mail watch --actor alice --timeout 30
```

`watch` blocks until mail addressed to you arrives, prints it oldest first, and exits 0; with `--timeout <s>` it exits 1 with empty stdout when nothing came. Its baseline is the moment it starts: already-unread mail does not trigger it -- drain `inbox --unread` first, or pass `--after <timestamp|id>` / `--include-existing` to have the backlog delivered. It never marks anything read; follow up with `read` or `ack`.
