---
name: ship
description: "Turn the current branch into a pull request: check Definition of Done, tidy commits into Conventional Commits, push, and open the PR from the repo template with Closes #n. Use when the work on an issue branch is done and verified."
---

# ship

Preconditions, in order — stop at the first that fails:

1. Branch matches `<type>/<n>-<slug>` and is not `main`.
2. `git status --porcelain` is empty (everything committed).
3. `/verify` ran green in this session (or run it now).
4. `.claude/rules/definition-of-done.md` checklist holds. Walk it explicitly.

## Steps

1. **Rebase on the fresh base** if behind. The base is what `start-issue` chose:
   `main`, or `epic/<epic>-<slug>` when the issue is `Part of #<epic>` with an epic lane
   (`git log --oneline origin/main..HEAD` shows epic commits you did not write → the
   base is the epic branch).
   ```bash
   git fetch origin <base>
   git rebase origin/<base>
   ```
   Resolve conflicts, re-run `/verify` if anything changed.
2. **Tidy commits.** Every commit must pass commitlint (`type(scope): subject`, lower-case
   subject, ≤ 72 chars, no trailing period). If the branch is a pile of WIP commits, squash
   to logical commits:
   ```bash
   git reset --soft $(git merge-base HEAD origin/<base>)
   git commit -m "feat(household): …"     # one per logical change
   ```
   Never do this after the branch has been reviewed — reviewers lose their anchors.
3. **Push.**
   ```bash
   git push -u origin HEAD
   ```
4. **Write the PR body** into a temp file following `.github/pull_request_template.md`:
   - **What** — one paragraph (use `/pr-summary` for the bullets under it).
   - **Why** — link the design doc section / issue discussion.
   - **Tests** — the verify table rows with counts, as printed. Name what was skipped.
   - **Review notes** — where to look first, trade-offs, follow-ups with issue numbers.
   - Last line: `Closes #<n>` (or `Refs #<n>` when the issue stays open).
   - **No AI attribution** of any kind — see Git Commit Policy in `CLAUDE.md`.
5. **Open the PR.** Title = Conventional Commits subject (it becomes the squash commit).
   ```bash
   gh pr create --base <base> --title "<type>(<scope>): <subject>" --body-file <tmp> \
     --label "<module>,<backend|frontend>"
   scripts/board.sh status <n> "In Review"
   ```
6. Print the PR URL and the next step: *"Review with the other tool: `$review-pr <pr>` in
   Codex / `/review-pr <pr>` in Claude Code."*

## Rules

- Do not merge. Do not approve. The owner does both. A feature PR is squash-merged
  (`--squash`); only an epic PR is rebase-merged — see `ship-epic`.
- Do not open a Draft unless the user asked; the PR is opened when the work is done.
