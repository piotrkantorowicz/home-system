---
name: ship-epic
description: "Close the epic lane: check every child is merged, rebase epic/<n>-<slug> onto fresh main, verify, and open the epic → main PR that is rebase-merged so each child commit stays on main. Use when the last child PR of an epic has merged."
---

# ship-epic

```
/ship-epic <epic-issue-number>
```

Preconditions — stop at the first that fails:

1. The epic issue lists an `Epic branch:` line and `origin/epic/<n>-<slug>` exists.
2. Every child in the epic's checklist is **closed** and its PR **merged**:
   ```bash
   gh issue view <n> --json body -q .body | grep -Eo '#[0-9]+' | sort -u   # children
   gh issue view <child> --json state,closedByPullRequestsReferences
   ```
   An open child means the epic is not done — say which, stop.
3. No open PR targets the epic branch: `gh pr list --base epic/<n>-<slug>`.

## Steps

1. **Check out the epic branch in a worktree** (never in a checkout that has other work):
   ```bash
   git fetch origin main epic/<n>-<slug>
   git worktree add .worktrees/epic-<n> epic/<n>-<slug>
   cd /abs/path/.worktrees/epic-<n>
   ```
2. **Rebase onto fresh main.** This is the one history rewrite the epic branch allows.
   ```bash
   git rebase origin/main
   ```
   Conflicts: resolve per commit, keep each child commit as one commit (no squashing
   across children — `main` must receive one Conventional Commit per child PR).
   `git rebase --abort` and report if a conflict needs a decision from the owner.
3. **Verify**: `/verify` (`scripts/verify.sh --branch`). Fix-ups that the rebase needs go
   into the commit they belong to (`git rebase -i` is not available to agents — use
   `git commit --fixup` on a child branch and a child PR instead; if it is a one-liner the
   owner can confirm, say so and stop).
4. **Push the sync**:
   ```bash
   git push --force-with-lease origin epic/<n>-<slug>
   ```
5. **Open the PR** epic → main. Title = the epic issue title as a Conventional Commit
   subject with the widest scope (`refactor(backend): …`, `feat(household): …`); it is
   only used for the PR — the commits on `main` are the child commits.
   Body from `.github/pull_request_template.md`:
   - **What**: one line per child, `- #<child> <title>` in merge order.
   - **Tests**: the verify table from step 3.
   - **Review notes**: `Merge with **Rebase and merge** — each child commit lands on main
     as-is; semantic-release computes the version from all of them.`
   - Last line: `Closes #<n>`.
   ```bash
   gh pr create --base main --head epic/<n>-<slug> --title "<subject>" --body-file <tmp> --label epic
   ```
6. **Board:** move the epic to `In Review`. Print the PR URL and the cross-review hint
   (`$review-pr` in Codex / `/review-pr` in Claude Code — review the integration, the
   children were already reviewed).

## After the owner merges

`gh pr merge <pr> --rebase --delete-branch` — owner-run, or agent-run only after
`reviewDecision == APPROVED` (the guard hook enforces `--rebase` for `epic/*` heads).
`release.yml` runs on the resulting push to `main` and tags the release. Remove the
worktree: `git worktree remove .worktrees/epic-<n>`.

## Rules

- Never squash the epic PR — that collapses N child commits into one and the release
  notes lose them.
- Never rebase while a child PR is open — its base would move under it.
