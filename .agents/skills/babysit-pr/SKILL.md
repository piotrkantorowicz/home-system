---
name: babysit-pr
description: "One tick of PR babysitting for /loop: check CI, merge conflicts and new review threads on a PR; fix CI failures, address new comments, and report when the PR is green and approved. Use as `/loop 10m /babysit-pr <n>` after shipping."
---

# babysit-pr

```
/babysit-pr <pr-number>
```

Designed to run repeatedly (`/loop 10m /babysit-pr <n>`). Each run is one tick. Do the
cheapest thing that moves the PR forward, then stop.

## Tick

1. **Status snapshot.**
   ```bash
   gh pr view <n> --json state,mergeable,mergeStateStatus,reviewDecision,statusCheckRollup,headRefName
   gh pr checks <n>
   ```
   If `state` is `MERGED` or `CLOSED`: say so and end the loop (`ScheduleWakeup stop` /
   tell the user to stop the loop).
2. **Conflicts** (`mergeable == CONFLICTING`): `git fetch origin main && git rebase origin/main`
   on the PR branch, resolve, `/verify`, `git push --force-with-lease`. (Force-with-lease is
   allowed on a feature branch only; the guard hook blocks anything else.) Note this in a
   PR comment since it moves review anchors.
3. **CI red**: find the failing job and read only the failed step:
   ```bash
   gh run list --branch <headRefName> --limit 3
   gh run view <run-id> --log-failed
   ```
   Fix, `/verify`, commit (`fix(...)`/`test(...)`), push. If the failure is flaky /
   infrastructure (Docker pull, npm registry), re-run once: `gh run rerun <run-id> --failed`.
4. **New unresolved review threads** since the last tick → run `/address-review <n>`.
5. **Green + approved** (`reviewDecision == APPROVED`, all checks pass, no unresolved
   threads): report *"ready to merge"* and end the loop. Do **not** merge — the owner does,
   or explicitly asks for `gh pr merge --squash --delete-branch`.
6. Otherwise print one status line — `checks: 3/4 ✅ · threads: 2 open · review: pending` —
   and end the tick.

## Rules

- Never approve, never merge on your own.
- Never rewrite history except the conflict rebase in step 2.
- Nothing to do → say "no change" so the loop's noop tracking stays accurate.
