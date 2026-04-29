# E2E Suite Fix After Settings/Navigation Refactors — Design

**Status**: Approved (sections 1 + 2)
**Date**: 2026-04-22
**Author**: brainstormed with user
**Sub-project of**: option D (full audit + restructure of `src/E2ETests/`)
**Issue**: TBD (to be opened after spec sign-off)

---

## Background

The Playwright e2e suite at `src/E2ETests/` has 87 tests across 13 specs with mature OIDC token-refresh fixtures, page-object models, and entity tracking. Recent UI refactors (#107 sidebar reduction, #108 user profile dropdown, #114 profile settings hub, #116 navigation cleanup) broke 35 tests and stranded 3 in chained skips.

Ground-truth run on 2026-04-22 (services on `localhost:5050` backend, `localhost:5173` frontend, `localhost:9000` Authentik):

| Spec | Pass / Total | Status |
|---|---|---|
| `dashboard.spec.ts` | 3/3 | green |
| `import.spec.ts` | 5/5 | green |
| `meals.spec.ts` | 6/6 | green |
| `pagination.spec.ts` | 8/8 | green |
| `products.spec.ts` | 5/5 | green |
| `recipes.spec.ts` | 4/4 | green |
| `nutrition.spec.ts` | 4/8 | 1 fail + 3 chained skips |
| `profile.spec.ts` | 5/7 | heading + sidebar nav |
| `hydration.spec.ts` | 2/6 | settings fields, save btn, sidebar nav, update flow |
| `meal-schedule.spec.ts` | 2/9 | most slot-mgmt tests |
| `notification-preferences.spec.ts` | 0/10 | **100% broken** |
| `weight-prediction.spec.ts` | 0/9 | **100% broken** |
| `theme.spec.ts` | 0/2 | toggle moved into user dropdown |
| `setup` | 1/1 | login still works |

This spec covers **D1 only** — restoring the suite to fully green. D2 (scenario documentation) and D3 (rules-vs-reality reconciliation) are tracked separately.

---

## Goals

1. All 87 tests pass against current `main` (no failed, no did-not-run, no skipped).
2. Fix specs and POMs to match the current UI; do **not** alter app code.
3. Remove dead test code and unused POM helpers spotted during the work.
4. Surface (do not paper over) any failure that indicates real behaviour change rather than a stale selector.

## Non-goals

- ❌ Scenario documentation — D2.
- ❌ Directory move (`src/E2ETests/` → `e2e/`), fixture barrel restructure, mergeTests pattern — D3.
- ❌ New test coverage for newly-added features (goals CTA card, mobile sidebar drawer, hydration quick-add widget). Future issue.
- ❌ Backend or React component changes.

---

## Scope

### In

- All files under `src/E2ETests/diet-planner/{spec.ts,page.ts}`.
- `src/E2ETests/diet-planner/utils/*` and `src/E2ETests/diet-planner/fixtures/*` if a spec or POM change requires it.
- `src/E2ETests/.env` only if test data setup is materially broken.

### Out

- App source under `src/ui/` — read-only for selector hunting.
- Backend under `src/Apis/` and `src/Modules/`.
- `playwright.config.ts` — no changes anticipated.

---

## Cluster fix strategies

### Cluster 1 — Profile hub migration (~32 of 35 failures)

**Affected specs**: `notification-preferences.spec.ts` (10), `meal-schedule.spec.ts` (most of 7), `hydration.spec.ts` settings tests (2), `profile.spec.ts` (1), `weight-prediction.spec.ts` (9 — separate root cause, see C4).

**Root cause**: PR #114 consolidated standalone settings routes into a single `/diet-planner/profile?section=<id>` hub. Specs still navigate to the defunct standalone routes.

**Fix**:

- Update each affected POM's `goto()` to land on the correct profile-hub section.
- Add shared helper `pages/profile-hub.helper.ts` exporting `gotoProfileSection(page, section)` that builds the `?section=` URL. Used by all settings POMs.
- Update `profile.page.ts` heading expectation: read the i18n value of `profile.page_title` (`src/ui/src/modules/diet-planner/locales/en.json`) and match against that.
- For `hydration.page.ts`: split methods conceptually into "hydration page" methods (`/diet-planner/hydration` — log entries, quick-add, progress) and "hydration settings" methods (`/diet-planner/profile?section=hydration`). Either two POMs or a `gotoSettings()` method on one — decide during implementation based on which reads cleaner.

### Cluster 2 — Sidebar reduction (delete dead tests)

**Affected**: `profile.spec.ts:84 › profile nav link is reachable from the sidebar`, `hydration.spec.ts:41 › hydration nav link is reachable from the sidebar`.

**Root cause**: PR #107 reduced sidebar to 4 core items. These tests assert reachability of items intentionally removed from the sidebar.

**Fix**: **Delete** both tests. The destinations are reached via other passing specs (`profile.spec.ts:5`, `hydration.spec.ts:5`) — coverage of the navigation path itself is implicit.

### Cluster 3 — Theme toggle relocation

**Affected**: `theme.spec.ts` (both tests).

**Root cause**: Theme toggle moved into `UserProfileDropdown` (per #108).

**Fix**: Update `theme.spec.ts` to open the user profile dropdown first, then click the toggle. Mirror the selector pattern used in `src/ui/src/shared/components/ui/UserProfileDropdown.test.tsx`.

### Cluster 4 — WeightPredictionCard prefill change

**Affected**: `weight-prediction.spec.ts` (all 9 tests).

**Root cause**: PR #116 changed WeightPredictionCard to prefill calorie input from goals data. The `getByLabel(/daily calorie target/i)` lookup now times out — the input is likely either renamed, hidden when prefilled, or in a different page location.

**Fix**: Inspect the current `WeightPredictionCard.tsx` rendering, locate the calorie input (or the prefilled state replacement), and update `weight-prediction.page.ts` accordingly. The card's location may also have moved (profile → dashboard) — verify and update navigation in `enterCalories()` and friends.

### Cluster 5 — Nutrition goal progress panel

**Affected**: `nutrition.spec.ts:95 › goal progress panel appears when nutrition goals are configured`, plus 3 chained did-not-run tests downstream.

**Root cause**: TBD — single failure, likely a selector tweak. Inspect the screenshot/error context from `test-results/diet-planner-nutrition-*-chromium/error-context.md` to diagnose.

**Fix**: Update the selector or assertion. Re-run downstream tests once unblocked to determine if they need their own fixes.

---

## Page object change matrix

| File | Change |
|---|---|
| `pages/profile.page.ts` | `goto()` lands on body-stats section explicitly. Heading regex matches `profile.page_title`. Audit unused methods. |
| `pages/notification-preferences.page.ts` | `goto()` URL → `/diet-planner/profile?section=notifications`. |
| `pages/meal-schedule.page.ts` | `goto()` URL → `/diet-planner/profile?section=meal-schedule`. |
| `pages/hydration.page.ts` | Split settings vs. page methods — either two POMs or a `gotoSettings()` overload. Settings methods land on `/diet-planner/profile?section=hydration`. Quick-add / progress methods stay on `/diet-planner/hydration`. |
| `pages/weight-prediction.page.ts` | Reconcile with #116 (prefilled calorie input, possibly relocated card). Update locators and `enterCalories()`. |
| `pages/calendar.page.ts`, `pages/dashboard.page.ts`, `pages/products.page.ts`, `pages/recipes.page.ts`, `pages/import.page.ts`, `pages/nutrition.page.ts` | No structural change. Audit for unused methods; remove what no spec calls. |

### New shared helper

`src/E2ETests/diet-planner/pages/profile-hub.helper.ts`:

```ts
import type { Page } from '@playwright/test';

export type ProfileSection =
  | 'body-stats'
  | 'goals'
  | 'meal-schedule'
  | 'hydration'
  | 'notifications';

export async function gotoProfileSection(page: Page, section: ProfileSection): Promise<void> {
  await page.goto(`/diet-planner/profile?section=${section}`);
  await page.waitForLoadState('networkidle');
}
```

Used by `profile.page.ts`, `notification-preferences.page.ts`, `meal-schedule.page.ts`, and the settings half of `hydration.page.ts`.

---

## Removal pass (dead code)

While doing the cluster fixes, remove anything obviously dead:

- The two cluster-2 spec blocks (sidebar nav reachability).
- POM methods that no spec calls (find via grep, e.g. `grep -r 'profilePage\.' diet-planner/`).
- Fixtures or utils with zero references.
- Setup helpers used by exactly one spec — inline if there's no reuse.

Each removal called out in the PR description so the reviewer can object to specific items.

---

## Verification

- `npx playwright test --reporter=list` — full suite, expect 87 passed / 0 failed / 0 did-not-run.
- After fixing a cluster, `npx playwright test diet-planner/<spec-name>.spec.ts` to confirm the cluster is green before moving on.
- Final pre-PR run of the previously-broken specs with `--repeat-each=2` to catch flakiness.
- Backend + frontend + Authentik must be running locally (no mocks).

---

## Risks & mitigations

1. **A "fix" hides a real regression**. Mitigation: if a spec's failure goes beyond a stale selector (page errors, action does nothing, value mismatch), stop and surface — do not paper over.
2. **OIDC token refresh flakiness**. The fixture refreshes mid-run, but an 11-min serial run is plenty for surprises. If a fix's spec passes once and fails on `--repeat-each=2`, suspect token-refresh edge cases before the fix itself.
3. **Backend state pollution**. `workers: 1` is in place; the entity tracker covers products/recipes but not other entities (goals, profile, hydration intake). Re-run flakes that the first run didn't show point to state leaks — surface, don't bury.
4. **i18n key drift**. Selectors that match translated strings break on rename. Prefer `getByRole + name` over `getByText` where possible. Flag any text-string selector added during this work.

---

## Commits & PR strategy

- **Branch**: `fix/<issue>-e2e-suite-after-settings-refactor` (per `.claude/rules/git-workflow.md`).
- **Commits**: one per cluster — five commits total (or six if removal pass is split).
- **PR title**: `fix(e2e): restore suite after settings hub and navigation refactors`
- **PR body** sections:
  - Summary
  - Changes (per cluster)
  - Removed (dead specs / POM methods, with justification per item)
  - Test plan (commands run + result counts)
  - `Closes #<issue>`
- No `Co-Authored-By: Claude` trailer. No "Generated with Claude Code" footer. (CLAUDE.md / saved feedback.)

---

## Follow-ups (not part of this issue)

- **D2** — write `docs/e2e-scenarios.md` cataloguing all 13 specs with purpose, acceptance criteria, gaps. Best done after D1 lands so the catalogue reflects reality.
- **D3** — reconcile the suite with `.claude/rules/frontend-playwright.md`. Either move the suite to match the rules (`e2e/` directory, fixtures barrel, `mergeTests`, etc.) or update the rules to describe current reality. Highest blast radius — do last; will conflict with anything in flight.
- **New coverage** — goals CTA card on dashboard, mobile sidebar drawer, hydration quick-add widget on calendar tab. None of these have e2e coverage yet.
