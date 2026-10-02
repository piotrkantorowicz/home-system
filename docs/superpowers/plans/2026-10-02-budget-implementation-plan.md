# Budget implementation plan — accepted review amendments

Status: **ready after documentation prerequisite #450 merges**

Updated: 2026-10-02

Epic: [#233](https://github.com/piotrkantorowicz/home-system/issues/233)

## Source and scope

[Budget design](../../design/budget/README.md) is the single authoritative specification, including full acceptance criteria and D1–D11 decisions. [Stage 1 review](../specs/2026-10-02-budget-design-review.md) records original findings and their accepted disposition. This plan describes implementation order and verification; it does not define competing business rules.

Keep issue IDs #446–#456 and one PR per issue. #450 changes from a separate duplicate endpoint to documentation publication. Duplicate backend filters belong in #448; duplicate UI belongs in #452. No feature implementation begins until #450's docs-only PR is merged to main.

## Order, files, and focused checks

All new backend files live under `src/Modules/Budget/`; UI under `src/ui/src/modules/budget/`. Reuse current Household module conventions and installed libraries. Read applicable `docs/rules/` files before writing code.

| Issue | Depends on | Work and likely files | Required checks |
|---|---|---|---|
| [#450](https://github.com/piotrkantorowicz/home-system/issues/450) — docs | None | Budget/Household READMEs, review disposition, this plan; synchronize epic and child bodies. | Markdown links, dependency graph, superseded behavior checks, docs-only verify. |
| [#446](https://github.com/piotrkantorowicz/home-system/issues/446) — initialize | #450; delivered #213/#234 | Budget projects/solution, DbContext/UoW/read-context, access helper, initialization API; host, Compose profile, E2E workflow and run/stop instructions. | One budget/default envelope under concurrent creation; role/household failures; correct module UoW and startup. |
| [#447](https://github.com/piotrkantorowicz/home-system/issues/447) — envelopes | #446 | BudgetAccount domain/configuration/migration; create/list/rename/archive/restore commands and endpoints. | Two-household privacy matrix, hidden counts/IDs, managed-to-linked transition, child-with-login privacy, revision conflicts. |
| [#448](https://github.com/piotrkantorowicz/home-system/issues/448) — expenses | #447 | Expense/share domain; immutable creation revision; decimal-string DTOs; create/list/detail; amount/category/date/excludeId filters; request identity constraints. | Exact equal splits, unsupported custom input rejected, actor scoping, concurrent retries, ±2-day duplicate filters, name snapshots, private counts. |
| [#449](https://github.com/piotrkantorowicz/home-system/issues/449) — edits/history | #448 | Expense update/void methods/commands; complete revision snapshots and queries; parent concurrency token and durable revision counter. | Atomic rollback, share-only edit race, create→edit→retry-create and edit→edit→retry-first-edit, retry after void, full prior shares, historical participant retention. |
| [#451](https://github.com/piotrkantorowicz/home-system/issues/451) — setup UI | #447 | Module index, API client/schema/query keys, setup/envelope screens, locales; registration in `app/main.tsx`. | Role/loading/error states, cache clearing, keyboard/mobile setup and envelope journey. |
| [#452](https://github.com/piotrkantorowicz/home-system/issues/452) — expense UI | #449, #451 | Entry/history/edit forms, authorized revision detail, list-filter hint, queries/mutations and locales. | Keep-both/cancel; hint failure is explicit; no create-time warning state; equal-subset preview; request ID preserved per logical submission; replay fetches current state. |
| [#453](https://github.com/piotrkantorowicz/home-system/issues/453) — limits | #448, #451 | MonthlyLimit configuration/commands; explicit-month summary projections; overview/envelope target UI. | Missing versus zero limit, no blocking on overspend, private totals, corrections moving months, concurrent limits, year/leap boundaries. |
| [#454](https://github.com/piotrkantorowicz/home-system/issues/454) — settlement | #448, #451 | All-recorded settlement projection, deterministic suggestions, former-participant names from snapshots; adult-only UI. | Zero-sum balances, no date cutoff, local-midnight/future-date cases, departed/demoted participants, shared-fund/personal exclusion, deterministic ties. |
| [#455](https://github.com/piotrkantorowicz/home-system/issues/455) — repayments | #454 | Immutable Settlement creation fields and request key; list/create/void API; confirmation/history UI. | Partial/overpayments, replay after void, real payments involving former participants, no fake write-off, no spending-total effect, role isolation. |
| [#456](https://github.com/piotrkantorowicz/home-system/issues/456) — final journeys | #452, #453, #455 | `e2e/budget/` POM/specs/seed-cleanup; lifecycle API tests where more precise; E2E documentation. | Real shared/private two-user journeys, child privacy, corrections/replays, repayments, removal/demotion/linking, complete DB start/stop wiring. |

Backend and UI slices remain independently reviewable. #451 may expose completed setup/envelope functions before expense entry exists; no unfinished action or route is advertised. Do not introduce a feature-flag framework or combine several PRs into one issue to claim fewer slices.

## Implementation details requiring care

1. **Replay ordering:** authorize current caller/resource; identify prior operation; compare immutable normalized request; return prior resource ID/applied revision if equal. Only new operations check expected revision and current roster eligibility. A revoked caller must not receive a historic result.
2. **Audit atomicity:** creation records the first full revision. Updates store the new state plus all shares while retaining the previous full revision. Even a share-only edit updates the parent expense so `xmin` concurrency applies. A lost race rolls back both history and current state.
3. **Snapshots:** expense payer/recorder names, share participant names, and repayment sender/recipient/recorder names live with their rows. Historical views use current roster names when available, snapshots otherwise; never add a write-on-read participant cache.
4. **Duplicate hints:** ordinary expense list, visible active rows first, exact amount/category and inclusive date range, exclude current expense on edit. Warning failure is not “no match”; saving without a hint requires an explicit user choice. Concurrent creations can still miss each other.
5. **Dates:** settlement counts every recorded active entry immediately. Explicit-month spending is unchanged. UTC timestamps remain for audit only; no implicit server “today” enters settlement math.
6. **Departures/demotion:** historical amounts never depend on today's roster. Adults can record actual repayments for historical participants; unpaid debt remains. No synthetic repayment or role-change blocker is introduced.
7. **Deferred work:** refunds/import, forgiveness, custom shares, cash accounts, and personal-history portability stay outside this release. Funding-source enum, managed-member support, and archive/restore stay in scope.

## Verification and delivery

For every issue, start from fresh main through the repository workflow. Read the rules listed in `AGENTS.md`; use the current module's existing patterns rather than copying stale examples blindly.

Run `scripts/verify.sh --branch` before each PR. Its path selection determines backend build/format/module tests, frontend lint/type-check/Vitest/build, and E2E type checks. For API contracts, regenerate and commit `npm run generate:api:budget` output once that script exists. UI changes also run the relevant real-stack Playwright journeys after stack startup; follow `docs/e2e/README.md` and `run-e2e` instructions.

The docs-only #450 change needs local-link and consistency checks plus `git diff --check`; path-aware verify should select no application suites. Do not claim application tests ran for documentation.

Use Conventional Commits and the PR template, one issue/PR, explicit `Closes #n`. Owner reviews and merges. PRs target the epic branch `epic/233-budget-module-household-scoped-expenses` (a dependent child whose parent is still in review stacks on the parent's branch and is retargeted when the parent merges); `ship-epic` rebase-merges the epic into `main`. Keep child tracking in GitHub sub-issues and update this table only when dependencies change.
