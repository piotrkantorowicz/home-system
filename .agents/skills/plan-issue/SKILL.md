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
   - **Labels** — exactly one type, at least one area or module, one priority
     (`.github/labels.json` is the source of truth, `scripts/sync-labels.sh` applies it):

     | Group | Labels |
     |---|---|
     | Type (one) | `enhancement` (feat), `bug` (fix), `tech-debt` (refactor/perf), `chore` (chore/ci), `documentation` (docs), `epic` |
     | Area | `backend`, `frontend`, `e2e`, `infra`, `ci` |
     | Module | `diet-planner`, `household`, `notifications`, `shared`, `ui-shell` |
     | Cross-cutting | `ux`, `accessibility` |
     | Priority (one) | `priority:high`, `priority:medium`, `priority:low` |
     | Flow | `blocked`, `needs-decision`, `hotfix` |

     The type label decides the issue template and the PR's Conventional Commits type;
     the release bump follows from that (see `agent-workflow.md` § Releases).
5. **Show the full list to the user and stop.** Titles, one-line scope each, dependency
   order. Wait for approval — this is a human gate.
6. **On approval**, create them in dependency order:
   ```bash
   gh issue create --title "<title>" --label "<a>,<b>" --body-file <tmpfile>
   ```
   Then make each one a **sub-issue** of the epic — the board groups by parent issue and
   shows *Sub-issues progress*; no body checklist to maintain:
   ```bash
   scripts/board.sh link <epic> <n>
   ```
7. **Board.** Approved slices are `Todo`; the epic moves from `Backlog` to `Todo`:
   ```bash
   scripts/board.sh add <n> Todo
   scripts/board.sh add <epic> Todo
   ```
   See § Project board below.
8. **Epic lane?** If the slices only make sense together (main would be inconsistent
   after a partial merge, or the feature is unusable until the last slice), say so and
   recommend `/start-epic <epic>`; the children then target `epic/<epic>-<slug>` instead
   of `main`. Independent slices go straight to `main` — the default.
9. Print the created issue numbers with titles.

## Project board

Issues are tracked on https://github.com/users/piotrkantorowicz/projects/3. The skills move
the Status column through `scripts/board.sh` (field ids resolved at run time):

| Moment | Skill | Status |
|---|---|---|
| Epic / idea filed, not planned | `board.yml` on issue opened | `Backlog` |
| Slice approved and created | `plan-issue` | `Todo` |
| Branch created | `start-issue` / `start-epic` | `In Progress` |
| PR opened | `ship` / `ship-epic` | `In Review` |
| PR opened | `board.yml` (from `Closes` / `Refs` in the body) | `In Review` |
| PR merged / issue closed | `board.yml` on issue closed | `Done` |

`scripts/board.sh statuses` lists the column names the board actually has (`Backlog`,
`Todo`, `In Progress`, `In Review`, `Done`). If a target column is missing the script exits 1
and names the existing ones — add the column on the board rather than renaming it here. The script uses
its own classic token (scopes `repo` + `project`) from `BOARD_TOKEN` or
`~/.config/home-system/board-token` — fine-grained PATs cannot reach user projects. Without
it, it warns and does nothing.

## Rules

- One issue = one PR. If a slice needs two PRs, it is two issues.
- Acceptance criteria are testable, not "works correctly".
- Do not write implementation code here. Planning only.
