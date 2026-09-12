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
```

## Stages

| Stage | Skill | Output | Gate |
|---|---|---|---|
| Plan | `plan-issue` | One GitHub issue per vertical slice, from the feature template, linked to the epic | Human approves the slice list before issues are created |
| Start | `start-issue <n>` | Branch `<type>/<n>-<slug>` off fresh `origin/main`, issue assigned | — |
| Implement | rule docs + scaffold skills | Code + tests per `definition-of-done.md` | — |
| Verify | `verify` | `scripts/verify.sh --branch` green, table in the transcript | — |
| Ship | `ship` | Pushed branch, PR from template, `Closes #n` | — |
| Bot review | `review-pr <n>` | Inline comments + one summary comment on the PR. Never approves. | **Run in the other tool** (see below) |
| Fix-up | `address-review <n>` | Fix commits, per-thread replies, resolved threads it changed | — |
| Babysit | `/loop 10m babysit-pr <n>` | Re-runs CI-fix / address-review until green + approved | — |
| Merge | owner | Squash merge, branch deleted | **Human approval required** — `guard-git.sh` blocks `gh pr merge` otherwise |

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
| `guard-git.sh` (PreToolUse Bash) | commit/push on `main`; `--force` push; `--force-with-lease` to `main`; `reset --hard`, `clean -f`, `checkout -- .`; `gh pr merge` without `reviewDecision == APPROVED` |
| `format-on-edit.sh` (PostToolUse Edit/Write) | nothing — runs Prettier on touched `src/ui` / `e2e` files so format checks never fail on style |

**Codex:** register the same scripts in `~/.codex/hooks.json` (`PreToolUse` / `PostToolUse`,
same JSON shape as Claude Code). They read the tool input from stdin and are tool-agnostic.

## Cloud review bots (not wired yet)

`anthropics/claude-code-action` (`@claude` on a PR) and the Codex GitHub app
(`@codex review`) give the same cross-review without a local session. Both need an API
key / subscription secret in the repo. When added, they replace the manual `review-pr`
step — `address-review` and the human gate are unchanged.
