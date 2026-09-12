---
name: plan-issue
description: "Turn an epic, a design-doc section, or a free-text feature request into one GitHub issue per vertical slice, each sized for exactly one PR. Use when asked to plan work, break down an epic, or create issues."
---

# plan-issue

Turn a plan into GitHub issues. Input is one of: an epic number (`#213`), a design-doc
path + section (`docs/design/household/README.md §8`), or a free-text description.

## Steps

1. **Read the source.** Open the design doc / epic body. Read the rule docs for the areas
   involved (Quick Reference table in `CLAUDE.md` / `AGENTS.md`).
2. **Check what already exists.** `gh issue list --state all --search "<keywords>"` and
   `gh issue list --label <module-label>`. Never create a duplicate; link to the existing
   issue instead.
3. **Slice.** Split into vertical slices — each slice is one branch, one PR, mergeable on
   its own, ideally < 1 day of work. Order them by dependency. Typical order for a feature:
   contracts / domain → application + endpoints → UI → e2e.
4. **Draft each issue** using the fields of `.github/ISSUE_TEMPLATE/feature.yml`:
   - **Title** — a valid Conventional Commits subject: `feat(household): …`,
     `fix(diet-planner): …`, `test(e2e): …`. It becomes the PR title and the squash commit.
   - **Context** — `Part of #<epic>. Design: <doc> §<n>.` plus one sentence of why.
   - **Scope** — bullets, split Backend / Frontend / Tests.
   - **Out of scope** — what is deferred and to which issue.
   - **Acceptance criteria** — checkable statements; each maps to a test or a manual check.
   - **Likely files / layers** — where to start.
   - **Test plan** — which unit / integration / e2e specs the PR must add.
   - **Labels** — module label (`household`, `notifications`, …), `backend` / `frontend`,
     `priority:*`, plus `enhancement` or `bug`.
5. **Show the full list to the user and stop.** Titles, one-line scope each, dependency
   order. Wait for approval — this is a human gate.
6. **On approval**, create them in dependency order:
   ```bash
   gh issue create --title "<title>" --label "<a>,<b>" --body-file <tmpfile>
   ```
   Then append a `- [ ] #<n> <title>` line per issue to the epic's body checklist
   (`gh issue view <epic> --json body`, edit, `gh issue edit <epic> --body-file`).
7. Print the created issue numbers with titles.

## Rules

- One issue = one PR. If a slice needs two PRs, it is two issues.
- Acceptance criteria are testable, not "works correctly".
- Do not write implementation code here. Planning only.
