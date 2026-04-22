# nutrition.spec.ts — Nutrition Summary

Mixed structure — three structure tests run independently; nine data-dependent tests run serially under a separate `describe` (`mode: 'serial'`, timeout 180000).

**Purpose**: nutrition summary page — date range picker, totals, daily averages, goal progress, table pagination.

**Setup**:

- Structure tests (3): no setup; assert UI affordances regardless of data.
- With-meal-data tests (9, serial): first `setup:` imports a weekly plan; later tests assert aggregates against it. The "goal progress panel" test additionally configures goals through `/diet-planner/profile?section=goals`.

## Tests — page structure

- `date range inputs are visible and default to the current week` — defaults match `startOfWeek(today, { weekStartsOn: 1 })`
- `selecting a past range with no meals shows the empty state`
- `Apply button is disabled when the from date is later than the to date`

## Tests — with meal data

- `setup: import a weekly meal plan` — seed
- `totals and daily average cards appear after applying the current week range`
- `daily breakdown table has at least one row for the current week`
- `switching to an empty date range shows the empty state`
- `goal progress panel appears when nutrition goals are configured` — configures goals via the profile hub, then asserts the panel is visible
- `pagination defaults to page size 25`
- `pagination: changing page size updates the selector value`
- `previous page button is disabled when on the first page`

## Acceptance

The nutrition summary correctly aggregates a week of meals, exposes goal-progress when goals exist, and exposes a paginated daily breakdown.

## Gaps

- Macro pie / bar visualisation
- Deficit / surplus calculation
- Exporting CSV
- Comparison against previous week
- Mobile layout
