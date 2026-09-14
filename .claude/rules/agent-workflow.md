# Agent Workflow — issue → branch → PR → review → merge

Both Claude Code (`/skill`) and Codex (`$skill`) drive the same loop with the same
skills from `.agents/skills/`. The human owns two gates: **approving the issue plan** and
**approving the PR**. Everything between is agent work.

```
 plan-issue ──► start-issue ──► (implement) ──► verify ──► ship
                                                             │
        ┌────────────────────────────────────────────────────┘
        ▼
   review-pr (OTHER agent) ──► address-review ──► owner review ──► merge (owner)
        ▲                                │
        └────────── babysit-pr (loop) ───┘
                                                                     │
                                                    push to main ──► release.yml (tag + GitHub Release)
```

Every issue lives on the project board (https://github.com/users/piotrkantorowicz/projects/3);
the skills move its Status column via `scripts/board.sh` — see `plan-issue` § Project board.
The script reads a classic PAT (`repo` + `project`) from `BOARD_TOKEN` or
`~/.config/home-system/board-token`; gh's own login stays a repo-scoped fine-grained PAT.

## Issues, labels, board

| Thing | Rule |
|---|---|
| Templates | `epic` (goal + done-when + epic-lane choice), `feature` (one PR), `bug`, `tech-debt`. Title is the Conventional Commits subject of the eventual PR. |
| Epic ↔ children | Children are **sub-issues** of the epic (`scripts/board.sh link <epic> <child>`), not a body checklist. The board groups by *Parent issue* and shows *Sub-issues progress*. |
| Labels | Source of truth `.github/labels.json`, applied by `scripts/sync-labels.sh` (`--prune` deletes unused extras). Exactly one **type** (`epic` / `enhancement` / `bug` / `tech-debt` / `chore` / `documentation`), ≥ 1 **area or module** (`backend` `frontend` `e2e` `infra` `ci` / `diet-planner` `household` `notifications` `shared` `ui-shell`), one **priority**, optional `ux` `accessibility`, flow `blocked` `needs-decision` `hotfix`. |
| Board Status | `Backlog` (filed, not planned) → `Todo` (approved by the plan gate) → `In Progress` (branch) → `In Review` (PR) → `Done` (merged / closed). |
| Board automation | `.github/workflows/board.yml` runs `scripts/board.sh` on events: issue opened → added as `Backlog`; reopened → `Todo`; closed → `Done` (a merged PR closes its issue); PR opened / ready → its `Closes` / `Refs` issues → `In Review`; PR closed unmerged → `Todo`. Needs the `BOARD_TOKEN` repo secret (same classic PAT). The skills move `Todo` → `In Progress` themselves. |
| Views | *Board* (by Status), *Epics* (table, `label:epic`, Sub-issues progress), *By epic* (board grouped by Parent issue), *Now* (Status in Todo/In Progress/In Review, sorted by priority). |

## Stages

| Stage | Skill | Output | Gate |
|---|---|---|---|
| Plan | `plan-issue` | One GitHub issue per vertical slice, from the feature template, linked to the epic | Human approves the slice list before issues are created |
| Start | `start-issue <n>` | Branch `<type>/<n>-<slug>` off fresh `origin/main` (or the epic branch, see below), issue assigned, board → In Progress | — |
| Implement | rule docs + scaffold skills | Code + tests per `definition-of-done.md` | — |
| Verify | `verify` | `scripts/verify.sh --branch` green, table in the transcript | — |
| Ship | `ship` | Pushed branch, PR from template, `Closes #n` | — |
| Bot review | `review-pr <n>` | Inline comments + one summary comment on the PR. Never approves. | **Run in the other tool** (see below) |
| Fix-up | `address-review <n>` | Fix commits, per-thread replies, resolved threads it changed | — |
| Babysit | `/loop 10m babysit-pr <n>` | Re-runs CI-fix / address-review until green + approved | — |
| Merge | owner | Squash merge, branch deleted | **Human approval required** — `guard-git.sh` blocks `gh pr merge` otherwise |
| Release | `release.yml` | Tag `vX.Y.Z` + GitHub Release from the commits since the last tag | automatic on every push to `main` |

## Epic lane (optional)

Default: every child issue of an epic merges straight into `main`. When the children only
make sense together — a refactor that leaves `main` inconsistent halfway, a feature unusable
until its last slice — open an **epic lane**:

```
 start-epic <e> ──► epic/<e>-<slug> on origin (from fresh main)
                         ▲  squash-merge          ▲  squash-merge
   start-issue <c1> ─► PR ┘    start-issue <c2> ─► PR ┘      (CI + hygiene as on main)
                         │
 ship-epic <e> ──► rebase onto main ──► PR epic → main ──► owner: Rebase and merge
```

| Stage | Skill | What happens |
|---|---|---|
| Open | `start-epic <e>` | `epic/<e>-<slug>` created on origin via API — no local commit. Noted on the epic issue as `Epic branch:`. |
| Children | `start-issue <c>` / `ship` | Detect `Part of #<e>` + the `Epic branch:` line → branch off and PR against the epic branch. Same review loop. Squash-merged into the epic: one Conventional Commit per child. |
| Close | `ship-epic <e>` | All children merged → rebase the epic onto fresh `main` (`--force-with-lease`, the one allowed push) → verify → PR `epic/… → main`, `Closes #<e>`. |
| Merge | owner | **Rebase and merge.** Each child commit lands on `main` as-is; `release.yml` analyses them one by one. Never squash an epic PR. |

Rules the tooling enforces:

- CI (`backend-ci`, `frontend-ci`, `pr-hygiene`) triggers on `epic/**` exactly like `main`.
  PR Hygiene also checks the base: `epic/*` heads target `main`; everything else targets
  `main` or an `epic/*` branch.
- `guard-git.sh` blocks `commit` / `merge` / `cherry-pick` / `revert` on an `epic/*`
  checkout and any push to an epic branch without `--force-with-lease`; `gh pr merge`
  must be `--rebase` for an `epic/*` head and `--squash` for everything else.
- Keep epics short. Rebasing the epic onto `main` rewrites its history, so it only happens
  in `ship-epic`, with no child PR open.

## Releases

`release.yml` runs semantic-release on every push to `main` (`release.config.js`):

| Commit type on `main` | Bump |
|---|---|
| `feat` | minor |
| `fix`, `perf`, `refactor`, `hotfix`, `revert` | patch |
| `type!` or `BREAKING CHANGE:` footer | major |
| `docs`, `style`, `test`, `chore`, `ci`, `build` | none |

The version is the tag (`vX.Y.Z`) plus a GitHub Release with generated notes; nothing is
committed back, so the bot never pushes to `main`. Squash-merged PRs contribute their
title; rebase-merged epics contribute every child commit. `npm run release:preview` at
the repo root shows the next version locally (needs `GITHUB_TOKEN=$(gh auth token)`).

## Cross-agent review rule

The agent that wrote the PR does not review it. Claude-written PR → `$review-pr` in
Codex. Codex-written PR → `/review-pr` in Claude Code. Different models have different
blind spots; a self-review mostly confirms the author's assumptions.

The bot review is advisory. It comments, it never approves — `reviewDecision` must come
from the owner.

## Parallel work

Use one git worktree per in-flight issue (`git worktree add .worktrees/<n>-<slug>`) so
Claude and Codex can work different issues at the same time without sharing an index.
`.worktrees/` is git-ignored.

## Guard rails (`.claude/settings.json` → `scripts/hooks/`)

| Hook | Blocks |
|---|---|
| `guard-git.sh` (PreToolUse Bash) | commit/push on `main`; `--force` push; `--force-with-lease` to `main`; `commit` / `merge` / `cherry-pick` / `revert` on `epic/*`; push to `epic/*` without `--force-with-lease`; `reset --hard`, `clean -f`, `checkout -- .`; `gh pr merge` without `reviewDecision == APPROVED`, or with the wrong strategy (`epic/*` → `--rebase`, else `--squash`) |

`guard-git.sh` checks the branch of the checkout the command targets: `git -C <dir>` wins, then a
leading `cd <dir>` (literal path — shell variables defined inside the same command are not
expanded), then the hook's cwd. In a worktree session, `cd` with the literal worktree path.
| `format-on-edit.sh` (PostToolUse Edit/Write) | nothing — runs Prettier on touched `src/ui` / `e2e` files so format checks never fail on style |

**Codex:** register the same scripts in `~/.codex/hooks.json` (`PreToolUse` / `PostToolUse`,
same JSON shape as Claude Code). They read the tool input from stdin and are tool-agnostic.

## Cloud review bots (not wired yet)

`anthropics/claude-code-action` (`@claude` on a PR) and the Codex GitHub app
(`@codex review`) give the same cross-review without a local session. Both need an API
key / subscription secret in the repo. When added, they replace the manual `review-pr`
step — `address-review` and the human gate are unchanged.
