---
name: handoff
description: "Save a summary of the current session to .claude/handoffs/ so it survives /clear, and flag it to auto-load into the next session. Use before /clear when the session has state worth carrying forward — an in-progress task, decisions made, or next steps."
---

# handoff

```
/handoff
```

Writes the working state of this session to disk so the next session (after `/clear`)
picks it up automatically — no manual re-explaining.

## Steps

1. Write a summary of the session to `.claude/handoffs/<UTC timestamp>.md`
   (`date -u +%Y%m%d-%H%M%S`), covering only what the next session actually needs:
   - **Task** — what was asked, one or two lines.
   - **Done** — what's finished (files touched, decisions made, why).
   - **In progress / next steps** — what's left, in order.
   - **Watch out for** — anything non-obvious the next session would otherwise rediscover
     the hard way (a false start, a constraint, a gotcha).
   Skip sections that are empty. This is a working note for an agent, not a report for a
   human — terse, no narrative.
2. Point the pending flag at it:
   ```bash
   mkdir -p .claude/handoffs
   echo "<filename>" > .claude/handoffs/.pending
   ```
3. Tell the user in one line: saved, and it'll load automatically after `/clear`.

## Auto-load

A `SessionStart` hook (matcher `clear`, `scripts/hooks/load-handoff.sh`) checks
`.claude/handoffs/.pending` on every `/clear`. If set, it injects the referenced file's
content as context for the new session and deletes the flag — so a handoff loads exactly
once, then the file stays in `.claude/handoffs/` as an inert archive.

## Rules

- Don't run this reflexively — only when there's real unfinished state. A session that
  ended cleanly (shipped, merged, nothing pending) has nothing worth handing off.
- One pending handoff at a time. Writing a new one overwrites which file `.pending` points
  to; older handoff files are not deleted, just no longer queued.
- `.claude/handoffs/` is git-ignored — it's local scratch, not project documentation.
