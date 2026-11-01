---
name: dependabot-pr-triage
description: Use when triaging Dependabot PRs: E2E secrets, major bumps.
category: dependency-management
---

# Dependabot PR triage

Diagnose each Dependabot failure before touching code. Most are config or scope problems, not test failures.

- **"Repository secret X is not set" is not a test failure.** Dependabot runs read only the Dependabot secret scope. Check with `gh secret list --app dependabot`; fix with `gh secret set <NAME> --app dependabot` using the same value as the Actions secret, then rerun.
- **Authentik startup timeout in E2E is a runner flake.** Rerun the job. Do not change code.
- **Major bumps break the build.** Fix the call sites on the bot branch, then run `/verify`. Known migrations: `references/major-bumps.md`.
- **Push fixes onto the `dependabot/*` branch**, not a new branch, so the PR updates. The pre-push branch-name check blocks `dependabot/*` until the exemption from #781 / PR #782 is merged. Do not `--no-verify` without asking.
- **Scratch worktree commits need `src/ui/node_modules`.** Without it the commit-msg hook fails with `npx canceled due to missing packages`. Symlink the main checkout's `node_modules` into the worktree.
- **Superseded PR:** close it with a link to the PR that now carries the same fix.
- **Merge is gated.** The merge guard needs an approving review or the `owner-approved` label. Agents do not add that label. Report the green PRs and ask the owner.
