---
name: start-issue
description: "Start work on a GitHub issue: create the correctly named branch off fresh origin/main, assign the issue, load the rule docs for its area, and restate scope + acceptance criteria. Use when asked to start, pick up, or begin an issue."
---

# start-issue

```
/start-issue <issue-number> [--worktree]
```

## Steps

1. **Load the issue.**
   ```bash
   gh issue view <n> --json number,title,body,labels,state
   ```
   If `state` is `CLOSED`, stop and say so.
2. **Derive the branch name** `<type>/<n>-<slug>`:
   - `type` = Conventional Commits type from the title prefix (`feat`, `fix`, `chore`,
     `refactor`, `docs`, `test`). `hotfix` only when the issue carries the `hotfix` label.
   - `slug` = title without the `type(scope):` prefix, lower-cased, kebab-case, ASCII only,
     ≤ 5 words. Must match `^(feat|fix|hotfix|chore|refactor|docs|test)/[0-9]+-[a-z0-9-]+$`.
3. **Branch off fresh main.**
   ```bash
   git fetch origin main
   git status --porcelain            # must be empty, otherwise stop and ask
   git checkout -b <branch> origin/main
   ```
   With `--worktree` (or when another issue is already in flight in this checkout):
   ```bash
   git worktree add .worktrees/<n>-<slug> -b <branch> origin/main
   ```
   and tell the user to open a session in that directory.
4. **Claim it.**
   ```bash
   gh issue edit <n> --add-assignee @me
   gh issue comment <n> --body "Started on branch \`<branch>\`."
   ```
5. **Load context.** From the labels / title, read the matching rule docs (Quick Reference
   in `CLAUDE.md` / `AGENTS.md`) and `.claude/rules/definition-of-done.md`. If the issue
   links a design doc section, read that section.
6. **Restate the job** in the transcript: scope bullets, acceptance criteria as a checklist,
   likely files. Flag anything in the issue that is ambiguous *before* writing code.

Then implement. When done: `/verify`, then `/ship`.
