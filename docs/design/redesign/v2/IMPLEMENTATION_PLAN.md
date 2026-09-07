# V2 implementation plan against the existing build

Reviewed: 6 September 2026. Scope: adopt the useful Today, hydration, and meal-plan improvements from v2 in the existing application.

## Evidence and limits

Inspected `src/ui/dist/index.html` and the compiled Dashboard (`Dashboard-BESVr1MO.js`), Hydration (`Hydration-D6pgiogA.js`), Calendar (`Calendar-BlfcHvIi.js`), and meal-query (`useMeals-DnVqFWgP.js`) chunks. Matched key behavior to the corresponding source files below. The compiled dashboard contains the same quick-add pending-key mismatch, and the compiled nutrition query returns an empty array on API errors.

This is a static build/source review, not an authenticated visual or backend test of `dist`. Source/build identity for every component is not established. Capture the authenticated build before implementation; do not treat v2's prototype screenshots or passing prototype tests as production evidence.

Edit `src/ui/src`, then rebuild `dist`. Do not edit hashed output. Preserve React Router, TanStack Query, current module registry, `/diet-planner/*` routes, and single-language i18n. The two-link v2 sidebar and fixed sample data are not production requirements.

## Already implemented: retain and reuse

- `shared/components/layout/AppShell.tsx`, `ModuleRail.tsx`, `SectionPanel.tsx`, `ModuleSwitcher.tsx`, and `CommandPalette.tsx`: two-tier navigation and module switching already exist.
- `modules/diet-planner/components/dashboard/NextUpCard.tsx`: already features the first uncompleted meal. Improve the remaining list rather than rebuilding this behavior.
- `components/GlassRow.tsx` and `WaterCustomAmountPopover.tsx`: shared water display and custom amount entry already exist.
- `pages/Hydration.tsx`: entry-specific deletion, inline confirmation, and Undo after adding already exist.
- `pages/Calendar.tsx`: day/week modes and shared `?date=&view=` state already exist. `components/calendar-day/DayView.tsx`, `DayMealList.tsx`, and `DaySummaryCard.tsx` provide an agenda foundation.
- `api/hooks/useMeals.ts`: complete and reset mutations already invalidate meals, nutrition, and shopping-list queries.

All module-relative paths below are under `src/ui/src/modules/diet-planner/` unless stated otherwise.

## PR 1 — Make displayed quantities trustworthy

**Priority: highest. Complete before changing nutrition labels or layout.**

### Separate planned and consumed nutrition

Current `pages/Dashboard.tsx` feeds `useNutritionSummary()` into `TodayHero.tsx`, which calls the value eaten. Backend `DietPlanner.Application/Queries/GetNutritionSummary/GetNutritionSummaryQueryHandler.cs` aggregates all matching entries, including Planned entries; status selects override nutrition but does not exclude unconsumed meals. `WeekReviewCard.tsx` then counts positive-calorie summary days as logged days.

Recommended implementation: inspect the meal DTO projection to confirm its nutrient values represent effective portions and overrides. If confirmed, derive **consumed** totals from Done/Modified meal DTOs and **planned** totals separately, using a module-local pure helper shared by Today and the day summary. Fetch the seven-day meal range once where practical and derive today's list from that data. Retain the existing nutrition-summary contract for its other consumers; do not silently change its meaning globally.

If DTOs cannot support accurate consumed totals, stop that frontend change at the contract boundary and specify an additive consumed-summary query/field as a separate backend task. A visual refresh must not substitute guessed values or reimplement unit conversion in the browser.

Acceptance: Planned meals do not increase consumed calories; Done meals do; Modified meals use actual nutrition; resetting a completed meal reduces consumed totals. Zero consumed meals read “Nothing logged,” not “Within budget.” Planned and consumed figures are explicitly labeled. Missing macro targets remain optional; zero targets never divide by zero.

### Distinguish loading, empty, and failed data

`useNutritionSummary()` currently returns `[]` on API error. Dashboard defaults missing goals to null and missing nutrition to zero; `WaterCard.tsx` also defaults unresolved values. Expose query failures and render local retry/error states instead of a false zero or premature set-goals prompt. Preserve usable neighboring panels when one query fails.

Files: `api/hooks/useMeals.ts`, `pages/Dashboard.tsx`, `components/dashboard/TodayHero.tsx`, `WaterCard.tsx`, `WeekReviewCard.tsx`.

### Keep calendar summaries on the selected date

`components/calendar-day/DaySummaryCard.tsx` always calls `useWaterIntake(formatToday())`, even when Calendar displays another date. Pass the selected ISO date through `DayView` to the summary. During date changes, do not present `keepPreviousData` meals under the newly selected date as if current: expose pending/placeholder state and suppress stale-date mutations until the new list arrives.

Acceptance: selecting yesterday shows yesterday's water and meals; delayed requests never allow edits to a previous day's meal under today's heading. Respect configured week start and active-language date formatting.

Tests: extend `api/hooks/useMeals.test.ts`, `pages/Dashboard.test.tsx`, `components/dashboard/TodayHero.test.tsx`; add focused tests for aggregation and selected-date summary behavior. No backend edits are assumed until DTO suitability is verified.

## PR 2 — Apply the Today composition

**Depends on PR 1.**

- `TodayHero.tsx`: replace the large ring and repeated remaining figure with the v2 compact calorie summary and four labeled macro bars. Reuse `MacroBar`; leave the shared `Ring` available to other screens.
- `pages/Dashboard.tsx`: nutrition full-width, then a wider meal agenda with water and week context beside it. Stack on narrow screens. Choose breakpoints using the available content width after the existing 280px shell, not the narrower prototype sidebar.
- `NextUpCard.tsx`: split upcoming and completed meals; keep the featured meal first; put Done/Modified meals in a keyboard-operable disclosure with a count. Use explicit status text instead of low-opacity strikethrough. Wrap long recipe names.
- Keep existing Add meal form, goals sheet, and direct calendar navigation. The prototype's omission of these controls is not a reason to remove them.
- Increase frequent action targets to at least 44px; use approximately 13px minimum supporting labels and 15px body text in affected screens. Use existing Button variants before introducing new variants.
- Localize new labels in `locales/en.json` and `locales/pl.json`; use the active language for date formatting instead of `undefined` locale.

Acceptance: next meal remains easy to find with many completed meals; completed rows can be expanded; empty and all-completed days offer useful actions. No page-level horizontal overflow at 390/768/1280/1600px. Long Polish names and 200% text zoom remain readable.

Tests: `TodayHero.test.tsx`, `NextUpCard.test.tsx`, `Dashboard.test.tsx`, plus `e2e/diet-planner/dashboard.spec.ts`.

## PR 3 — Make water actions explicit and reliable

**Can be developed after PR 1 independently of the visual composition.**

Current `WaterCard.tsx` stores pending keys shaped like `add-250-<timestamp>` but checks `pendingKeys.has('add-250')`. Hydration has the same pattern. These disabled checks cannot match. All filled `GlassRow` cells call the same remove-newest handler, although their displayed volumes are not individual entries. Dashboard removal and add-Undo deletion also lack local error feedback.

Plan:

1. Keep add controls and the existing custom popover. Replace ambiguous delete-on-glass behavior with informational progress plus an entry list. Reuse the Hydration list behavior on Today via a module component if that avoids actual duplication.
2. Make pending policy explicit: disable only the amount currently being added, keep other amounts usable, and maintain per-entry deletion state. Use matching keys or counters; do not use wall-clock timestamps as unique operation identity.
3. Preserve Undo after successful addition using the exact returned entry ID. Report failed Undo and deletion with actionable error text; do not announce success before the mutation succeeds.
4. Keep inline confirmation for deletion. **Do not copy v2's Undo-removal promise:** the current log API accepts date/amount/note but not the original timestamp or ID. Re-adding would create a different entry. Exact restoration requires a separate API capability; it is deferred from this frontend scope.
5. Respect `trackWaterIntake`, user target, glass size, and custom amounts. Preserve notes and chronological entry labels.

Files: `components/dashboard/WaterCard.tsx`, `pages/Hydration.tsx`, `components/GlassRow.tsx`, `WaterCustomAmountPopover.tsx`, `api/hooks/useHydration.ts`. Inspect `HydrationQuickAdd.tsx` for equivalent behavior and align only where necessary.

Acceptance: two equal-volume entries remain individually removable; repeated taps cannot unintentionally duplicate an in-flight amount; other amount controls remain usable; failed add/delete/Undo is visible; dashboard and Hydration converge after query invalidation.

Tests: `GlassRow.test.tsx`, `WaterCustomAmountPopover.test.tsx`, `api/hooks/useHydration.test.ts`, `pages/hydrationToast.test.tsx`, `e2e/diet-planner/hydration.spec.ts`.

## PR 4 — Finish meal completion and mobile planning

### Completion Undo

Add an action to the successful `NextUpCard` completion toast using existing `useResetMeal()`. Offer it only for the Planned → Done transition initiated by that action. Reset clears override data, so it must not serve as a general restore operation for Modified meals. Avoid resetting a meal subsequently edited through another visible flow; keep Undo short-lived and invalidate it when local state has moved on. A cross-client race cannot be fully prevented without a conditional backend mutation; do not claim that guarantee.

Show per-meal pending state, disable duplicate completion of the same entry, report reset failure, and retain focus after the completed row moves into the disclosure. Reuse toast conventions; avoid globally changing toast timing for unrelated screens.

### Mobile agenda

`WeekGrid.tsx` intentionally has `min-w-[840px]` inside horizontal scrolling. Keep its desktop comparison behavior. Add a seven-day selector over existing `DayView`; use the agenda as the narrow-screen default when `view` is absent. Explicit URL view choices take precedence. Resizing must not reset date or pollute browser history.

Keep add/edit/delete, overrides, completion/reset, configured meal slots, week start, and current query parameters. Do not introduce month view or DnD as part of v2.

Files: `pages/Calendar.tsx`, `components/calendar-day/DayView.tsx`, `DayMealList.tsx`, `DaySummaryCard.tsx`, optional module-local day-selector component; `NextUpCard.tsx` and existing complete/reset hooks.

Acceptance: deep-linked dates work on mobile; selecting Sunday respects week-start preference; browser Back restores explicit view changes; every meal action remains available; Undo updates both quantities and status.

Tests: `NextUpCard.test.tsx`, `pages/calendarToast.test.tsx`, existing calendar/day-view tests and `e2e/diet-planner/meals.spec.ts`. Add targeted navigation coverage only where current tests do not cover the selector.

## PR 5 — Week context, legibility, and final proof

- `WeekReviewCard.tsx`: retain the existing rolling last-seven-days range rather than copy the prototype's fixed Monday–Sunday week. Replace missing-day 30% stubs with a baseline/explicit no-log state. Show exact values accessibly and label today as partial. Derive logged-day counts and averages from the consumed-data semantics agreed in PR 1.
- Check chart geometry in the real browser: the percentage bars are nested in flex children without an obvious fixed per-column height. Treat this as a rendering check, not an already reproduced bug. Give the plotting area a definite height if needed.
- `src/ui/src/index.css`: measure text/background contrast before changing semantic muted colors. Use existing stronger `text-text-2` for important secondary content where sufficient. Any global token change requires a light/dark sweep of other routes; avoid a broad palette rewrite.
- Preserve the module shell. At 768px, inspect content width with its expanded section panel. Use its existing collapse behavior where appropriate before inventing another shell.
- Mobile `navModel.ts` currently returns every registered destination to the horizontally scrolling `BottomTabBar`. Treat a small stable primary set plus a “More” menu as a separate follow-up if real-device review confirms poor discoverability. Every destination and notification badge must remain accessible. Do not hard-code diet routes into shared layout.

## Delivery and validation

Recommended merge order: **PR 1 → PR 2 → PR 3 → PR 4 → PR 5**. Keep each PR independently reviewable and reversible. No production source or build output was changed during this planning task.

Before implementation, capture the authenticated current build on Today, Hydration, and Calendar in both languages and themes. Use real or controlled test records covering empty, planned-only, Done, Modified, above-target, missing-goal, and partial-water states. Confirm the build's source revision before using it as the visual baseline.

For each PR, run focused Vitest tests and `npm run type-check` / `npm run lint` in `src/ui`. Before delivery run `npm run build`, relevant Playwright scenarios against the real backend and Authentik, and a viewport/keyboard review at 390/768/1280/1600px, including expanded/collapsed desktop navigation. Read the repo's run-e2e skill and test rules when executing those checks.

Stop condition: correct planned/consumed quantities; no false empty state on failures; compact responsive Today; explicit water-entry actions; reliable supported Undo; mobile agenda retaining existing workflows; passing relevant checks. Products, recipes, import, preferences, notification workflows, new dependencies, and backend schema changes remain outside the design implementation unless a documented prerequisite requires a separately scoped change.
