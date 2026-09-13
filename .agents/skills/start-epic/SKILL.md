---
name: start-epic
description: "Open the epic lane for a multi-PR epic: create epic/<n>-<slug> on origin from fresh main, note it on the epic issue, and move the epic on the project board. Use when an epic's children should merge into a shared branch instead of straight to main."
---

# start-epic

```
/start-epic <epic-issue-number>
```

Use it when the children of an epic must ship together (a refactor that leaves `main`
inconsistent halfway, a feature that is useless until its last slice lands). Independent
children keep going straight to `main` — do not open an epic lane for those.

## Steps

1. **Load the epic.** `gh issue view <n> --json title,labels,state,body`. It must be open
   and carry the `epic` label. Refuse otherwise.
2. **Derive the branch name** `epic/<n>-<slug>` — same slug rules as `start-issue`
   (kebab-case, ASCII, ≤ 5 words, from the title without the `[Epic]` prefix). Must match
   `^epic/[0-9]+-[a-z0-9-]+$`.
3. **Create it on origin from fresh main** — no local checkout, no local commit:
   ```bash
   sha=$(gh api repos/{owner}/{repo}/git/ref/heads/main -q .object.sha)
   gh api repos/{owner}/{repo}/git/refs -f ref="refs/heads/epic/<n>-<slug>" -f sha="$sha"
   git fetch origin epic/<n>-<slug>
   ```
   If the ref already exists, stop and report it — never recreate.
4. **Record it** on the epic issue, under the children checklist:
   ```
   Epic branch: `epic/<n>-<slug>`. Children target it; it rebase-merges into `main` via `ship-epic`.
   ```
   (`gh issue view <n> --json body`, append, `gh issue edit <n> --body-file`).
5. **Board:** move the epic to `In Progress` (see `plan-issue` § Project board).
6. Print the branch and the next step: `/start-issue <child>` — it detects the epic branch
   from `Part of #<n>` in the child's body and branches off it.

## Rules

- The epic branch only ever receives squash-merged child PRs and the rebase sync in
  `ship-epic`. `guard-git.sh` blocks `commit` / `merge` / `cherry-pick` on it and any
  push to it without `--force-with-lease`.
- CI runs on it like on `main` (`epic/**` in every workflow trigger); child PRs get
  Backend / Frontend CI + PR Hygiene against the epic base.
