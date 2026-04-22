# nutrition.spec.ts — Nutrition Summary

Mixed structure — three structure tests run independently; eight data-dependent tests run serially under a separate describe (`mode: 'serial'`, timeout 180000).

**Purpose**: nutrition summary page — date range picker, totals, daily averages, goal progress panel, table pagination.

**Setup**:

- **Structure describe** (3 tests): no setup; assert UI affordances regardless of data.
- **With-meal-data describe** (8 tests, serial): the first `setup:` test imports a weekly plan via the import wizard. Later tests assert aggregates against that data. The `goal progress panel` test additionally configures goals through `/diet-planner/profile?section=goals` (uses raw `page.locator('#dailyCalorieTarget')` etc., not a POM).

**POM**: `pages/nutrition.page.ts`:

- `fromInput`, `toInput`: `getByTestId('from-date-picker' / 'to-date-picker')`
- `applyButton`: `getByRole('button', { name: /apply/i })`
- `tableRows`: rows in the daily-breakdown table that don't contain `<th>`
- `pageSizeSelect`: `getByRole('combobox')` with fallback to `select` (the page may render either depending on the design system component used)
- `setDateRange(from, to)` uses an internal `selectDate(testId, dateStr)` helper that drives the Radix popover-backed react-day-picker via month/year `<select>`s, then clicks the day button via `evaluate()` (Radix portal pointer events don't reach React via Playwright's CDP click)
- `applyRange(expectedFrom, expectedTo)` registers `waitForResponse` with a URL filter that matches the expected `from` / `to` query params before clicking Apply — so the test only proceeds once the API call completed with the right range

## Tests — page structure (independent)

### `date range inputs are visible and default to the current week`

- **Given** the user is on `/diet-planner/nutrition`
- **When** the page settles (after the GET `/nutrition-summary` request)
- **Then** from / to inputs and the Apply button are visible, and the picker `data-value` attributes match `format(startOfWeek(new Date(), { weekStartsOn: 1 }), 'yyyy-MM-dd')` and `+6 days`
- **Notes**: validates the default-week math against `date-fns` — Monday is the week start.

### `selecting a past range with no meals shows the empty state`

- **Given** the user is on the nutrition page
- **When** the user sets the range to `2020-01-01 → 2020-01-07` and clicks Apply
- **Then** `expectEmptyState()` — `/no meal data/i` is visible
- **Notes**: 2020 is far enough in the past that the test user can't have meals there; this is the most reliable way to trigger the empty state without test-data manipulation.

### `Apply button is disabled when the from date is later than the to date`

- **Given** the user is on the nutrition page
- **When** the user sets `from: 2026-12-31`, `to: 2026-01-01`
- **Then** the Apply button is disabled
- **Notes**: client-side validation — no API call is made.

## Tests — with meal data (serial)

### `setup: import a weekly meal plan`

- **Given** the user navigates to `/diet-planner/import`
- **When** the wizard is run with `generateWeeklyPlan(new Date())` (4 meal types × 7 days)
- **Then** the URL becomes `/diet-planner/calendar`
- **Notes**: this seeds the data for all subsequent tests in the describe. If it fails, every subsequent test cascades.

### `totals and daily average cards appear after applying the current week range`

- **Given** the seed has run
- **When** the user navigates to the nutrition page (defaulting to the current week)
- **Then** `expectTotalsVisible()` (`/totals/i`) and `expectDailyAvgVisible()` (`/daily average/i`) are both visible
- **Notes**: doesn't assert specific values — just that the aggregates render.

### `daily breakdown table has at least one row for the current week`

- **Given** the seed exists
- **When** the user is on the nutrition page
- **Then** `tableRows.first()` is visible within 8 s
- **Notes**: the row count isn't asserted — only that the table is non-empty.

### `switching to an empty date range shows the empty state`

- Same as the structure-test version of this assertion, but inside the data-dependent describe so that the seed is in place when the test runs (proves switching from non-empty to empty correctly clears the table).

### `goal progress panel appears when nutrition goals are configured`

- **Given** the seed exists
- **When** the user navigates to `/diet-planner/profile?section=goals`, alternates protein between `150` and `140` (to guarantee dirtiness), fills `dailyCalorieTarget: 2000`, `carbsGrams: 220`, `fatGrams: 70`, optionally `fiberGrams: 30` (if the field exists), clicks `Save goals`, then navigates to the nutrition page
- **Then** `expectGoalProgressVisible()` — `/goal progress/i` is visible within 8 s
- **API**: `PUT` to the goals endpoint
- **Notes**: cross-spec state — saving goals here persists for the rest of the run and affects [weight-prediction](weight-prediction.md) (calorie input prefill).

### `pagination defaults to page size 25`

- **Given** the user is on the nutrition page
- **When** the page settles
- **Then** `pageSizeSelect.value === '25'`

### `pagination: changing page size updates the selector value`

- **Given** the user is on the nutrition page
- **When** `setPageSize(10)` runs, then `setPageSize(50)`
- **Then** `pageSizeSelect.value` becomes `10`, then `50`
- **Notes**: doesn't assert the table changed — only the selector state.

### `previous page button is disabled when on the first page`

- **Given** the user is on the nutrition page (defaults to page 1)
- **When** the page settles
- **Then** `previousButton` is disabled

## Acceptance

The nutrition summary correctly aggregates a week of meals, exposes goal-progress when goals exist, and provides paginated daily breakdown with date-range filtering.

## Gaps

- Macro pie / bar visualisation (covered only as text totals)
- Deficit / surplus calculation against goals (the goal-progress panel is asserted as visible but its content isn't read)
- Exporting CSV
- Comparison against previous week
- Mobile layout (compact / horizontal-scroll table)
- Custom date ranges (single day, full month, full year)
- Specific aggregate values for known meal data
- Sorting columns in the daily breakdown table
- Next-button enabled state at page > 1
