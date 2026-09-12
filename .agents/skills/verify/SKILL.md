---
name: verify
description: "Run the path-aware verification (build, dotnet format, module tests, ui lint/type-check/vitest/build, e2e tsc) for the current branch via scripts/verify.sh and fix what fails. Use before opening a PR, after addressing review comments, or when asked whether the branch is green."
---

# verify

```
/verify [--no-integration] [--all]
```

## Steps

1. Run the shared script. It picks checks from the changed paths vs `origin/main`:
   ```bash
   scripts/verify.sh --branch            # default
   scripts/verify.sh --branch --no-integration   # fast loop, no Docker
   scripts/verify.sh --all               # everything, CI parity
   ```
2. Read the summary table at the end. For every ❌:
   - read the failing output above the table,
   - fix the root cause (never skip / `[Fact(Skip=…)]` / `it.skip` a failing test to go green),
   - re-run.
3. Repeat until every selected row is ✅ or ⏭ with a stated reason.
4. Paste the final table into the transcript. This is what goes into the PR "Tests" section
   — with the counts the test runners printed.

## Rules

- Never report green without a run in this session.
- ⏭ rows (no Docker, `--no-integration`) must be named in the PR as not run locally; CI
  runs them anyway.
- `dotnet format` failures: run `dotnet format HomeSystem.slnx` and commit the result as
  its own `style(...)` commit.
