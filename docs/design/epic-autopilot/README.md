# Epic autopilot — design

Status: proposed (#800). Owner: repo owner.

## 1. Goal

Once an epic has a spec and planned child issues, deliver it with no human in the loop until
the epic → `main` PR:

1. start children one by one or in parallel, respecting dependencies;
2. have a second agent review each child PR;
3. use a different model per role — read/find, implement, review/advise;
4. fix review comments, re-review, and squash-merge each child into the epic branch;
5. ship the epic PR, which the owner reviews and rebase-merges.

**Done when** `/deliver-epic <e>` takes a planned epic to an open epic → `main` PR, or parks
the children it cannot finish with a written reason and a notification.

**Not changing:** `main` stays owner-gated. Every merge into `main` still needs
`reviewDecision == APPROVED` or the owner's `owner-approved` label.

## 2. Decisions

| # | Decision | Rejected |
|---|---|---|
| D1 | Merge gate for children: `guard-git.sh` allows `gh pr merge --squash` into `epic/*` on green CI + 0 unresolved threads + a reviewer approval marker for the current head SHA. | Separate bot account / GitHub App submitting real approvals (more setup); keeping a human merge per child (no autonomy). |
| D2 | Dependencies are GitHub native issue dependencies ("blocked by"). Unblocked children run in parallel, **max 2** at a time. | Strict sequential (slow); uncapped (Testcontainers contention, sibling conflicts). |
| D3 | Runtime is a skill in a Claude Code session; the session coordinates, subagents do the work. State lives on GitHub, so a rerun resumes. | Headless `claude -p` script (no live view, unattended permissions); cloud routine (no Docker / local stack). |
| D4 | Stuck child: 3 review rounds / 3 CI-fix attempts, then one advisor attempt, then park with `needs-decision`, notify, and keep running unblocked siblings. | Halting the whole epic on the first stuck child. |

## 3. Roles

Agent definitions live in `.claude/agents/<name>.md`; `model:` in the frontmatter picks the tier.

| Role | Definition | Model | Tools | Job |
|---|---|---|---|---|
| Coordinator | `/deliver-epic` skill | session model | `gh`, `Agent`, `scripts/board.sh` | Reads the graph, starts children, routes stages, merges, ships. Holds no code context. |
| Scout | `epic-scout` | haiku | read-only (`Read`, `Grep`, `Glob`, `Bash` for `gh`/`git log`) | Writes the child brief: issue, spec section, rule docs to read, likely files, conflict risks (e.g. migrations in the same module as a running sibling). Also summarises failing CI logs. |
| Builder | `epic-builder` | sonnet | all, `isolation: worktree` | `start-issue` → implement → `verify` → `ship` (PR into `epic/<e>-<slug>`); later `address-review` and CI fixes on the same branch. |
| Reviewer | `epic-reviewer` | opus | read-only + `gh pr comment/review` | `review-pr`; ends the summary comment with the verdict marker (§5). |
| Advisor | `epic-advisor` | opus | all | Called only for a stuck child: one attempt to decide, fix, or park with a reason. |

Opus reviewing Sonnet's work satisfies the cross-agent review rule in `agent-workflow.md`.
Codex review remains possible but is not required by the autopilot.

## 4. Flow

```
/deliver-epic <e>
  ├─ ensure epic lane (start-epic if missing)
  ├─ load children (sub-issues) + blocked_by edges; skip merged/closed
  └─ loop while open children exist:
       pick up to 2 children whose blockers are all merged into the epic
       for each (in parallel):
         scout(brief)
         builder(start-issue → implement → verify → ship)      PR base: epic/<e>-<slug>
         wait CI (gh pr checks --watch)        ─ red ─► scout(log) → builder(fix)   ≤3
         reviewer(review-pr)                    ─ request-changes ─► builder(address-review) → CI → reviewer   ≤3
         approve on head SHA + CI green + 0 unresolved ─► gh pr merge --squash --delete-branch
         stuck ─► advisor ─► still stuck ─► label needs-decision, comment reason, PushNotification, park
  all children merged ─► ship-epic ─► epic → main PR (owner merges, --rebase)
```

- A child branches from the **fresh epic head** after its blockers merged, so it builds on them.
- Two running children that touch the same module's EF migrations are serialised: the scout
  flags it and the coordinator holds the second one back.
- The coordinator never edits code. Every long wait (CI, review) runs inside a subagent, so the
  coordinator's context only grows by one line per stage.

## 5. Approval marker

The reviewer's summary comment ends with one line:

```
<!-- epic-review: approve sha=<40-char head sha> -->
<!-- epic-review: request-changes sha=<40-char head sha> -->
```

- Only the **newest** marker on the PR counts, and only when its `sha` equals the PR's current
  `headRefOid`. Any push after approval invalidates it; the reviewer runs again.
- `review-pr` keeps its rule of never submitting a GitHub APPROVE review; the marker is a comment.
- Trust level matches `owner-approved`: a best-effort guard against agent mistakes, not a
  security boundary. Hardening, if the hook input exposes it: accept the marker only from
  comments posted while `agent_type == epic-reviewer`, and block builders from writing the string.

## 6. Changes to existing pieces

| Piece | Change |
|---|---|
| `scripts/hooks/guard-git.sh` | New branch for `gh pr merge` when `baseRefName` matches `epic/*` and the head is not `epic/*`: require `--squash`, all required checks `SUCCESS`, 0 unresolved review threads, newest marker = `approve` with `sha == headRefOid`. Everything else unchanged. |
| `scripts/tests/test_guard_git.py` | Cases: marker approve + current SHA → allowed; stale SHA → blocked; request-changes newest → blocked; unresolved thread → blocked; failing check → blocked; base `main` with marker → still blocked. |
| `.agents/skills/plan-issue` | Write order as `blocked_by` links (`gh api repos/:o/:r/issues/<n>/dependencies/blocked_by -f issue_id=<id>`); recommend the epic lane when the epic will run on autopilot. |
| `.agents/skills/review-pr` | Emit the §5 marker as the last line of the summary comment. |
| `.agents/skills/deliver-epic` | New coordinator skill (§4), with `--dry-run` printing the plan and the next actions without writing anything. |
| `.claude/agents/` | New `epic-scout`, `epic-builder`, `epic-reviewer`, `epic-advisor`. |
| `docs/rules/agent-workflow.md` | New "Epic autopilot" section linking here; merge row notes the epic-child exception. |

## 7. State and resume

No local state file. Each child's state is derived from GitHub on every pass:

| Observed | State | Next action |
|---|---|---|
| Issue closed / PR merged into epic | done | — |
| Label `needs-decision` | parked | skip; owner unparks by removing the label |
| Open blocker | blocked | wait |
| No branch / no PR | ready | scout → builder |
| PR open, checks red | ci-failing | scout(log) → builder(fix) |
| PR open, newest marker missing or stale SHA | needs-review | reviewer |
| PR open, newest marker request-changes | changes-requested | builder(address-review) |
| PR open, approve on head + green + resolved | mergeable | merge |

Round counters (review rounds, CI fixes) are counted from markers and fix commits on the PR, so
they survive a restart too.

## 8. Guardrails

- Existing guards stay: no commits on `main` / `epic/*`, no force-push, epic pushed only by `ship-epic`'s `--force-with-lease`.
- Merge into `main` is never automated.
- Parallel cap 2; each builder in its own worktree (`.worktrees/<n>-<slug>`), removed after merge.
- Park sends `PushNotification` with the PR link and the advisor's reason.
- `--dry-run` before the first real run of any epic.

## 9. Testing

- `test_guard_git.py` cases from §6.
- `/deliver-epic <e> --dry-run` on a finished epic → reports everything done; on #793 → prints the plan.
- First real run: epic #793 (invested capital). PR #799 is retargeted onto `epic/793-…` first.

## 10. Delivery

One PR each, in order:

1. `feat(agents): gate epic child merges on reviewer approval marker` — guard + tests + `review-pr` marker.
2. `feat(agents): record child order as issue dependencies` — `plan-issue`.
3. `feat(agents): add scout, builder, reviewer and advisor agents` — `.claude/agents/`.
4. `feat(agents): add deliver-epic coordinator skill` — skill, `--dry-run`, workflow doc section.
