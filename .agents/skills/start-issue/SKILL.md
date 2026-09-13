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
3. **Pick the base.** Default `origin/main`. If the issue is a sub-issue of an epic (or
   its body says `Part of #<epic>`) and that epic has an epic lane, base on it instead:
   ```bash
   epic=$(gh api graphql -F owner=:owner -F name=:repo -F n=<n> -f query='query($owner:String!,$name:String!,$n:Int!){
     repository(owner:$owner,name:$name){ issue(number:$n){ parent{ number } } } }' -q '.data.repository.issue.parent.number // empty')
   [[ -n "$epic" ]] || epic=$(gh issue view <n> --json body -q .body | grep -Eo 'Part of #[0-9]+' | grep -Eo '[0-9]+' | head -1)
   base=$(gh issue view "$epic" --json body -q .body | grep -Eo 'Epic branch: `epic/[0-9]+-[a-z0-9-]+`' | tr -d '`' | cut -d' ' -f3)
   base=${base:-main}
   ```
   Say which base was chosen — the PR in `ship` must target the same one.
4. **Branch off the fresh base.**
   ```bash
   git fetch origin <base>
   git status --porcelain            # must be empty, otherwise stop and ask
   git checkout -b <branch> origin/<base>
   ```
   With `--worktree` (or when another issue is already in flight in this checkout):
   ```bash
   git worktree add .worktrees/<n>-<slug> -b <branch> origin/<base>
   ```
   and tell the user to open a session in that directory.
5. **Claim it.**
   ```bash
   gh issue edit <n> --add-assignee @me
   gh issue comment <n> --body "Started on branch \`<branch>\` (base \`<base>\`)."
   scripts/board.sh add <n> "In Progress"
   ```
6. **Load context.** From the labels / title, read the matching rule docs (Quick Reference
   in `CLAUDE.md` / `AGENTS.md`) and `.claude/rules/definition-of-done.md`. If the issue
   links a design doc section, read that section.
7. **Restate the job** in the transcript: scope bullets, acceptance criteria as a checklist,
   likely files. Flag anything in the issue that is ambiguous *before* writing code.

Then implement. When done: `/verify`, then `/ship`.
