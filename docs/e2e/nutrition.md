# nutrition.spec.ts — Nutrition Summary

Two describes: **page structure** (3 tests, independent) and **with meal data**
(9 tests, serial, `mode: 'serial'`, timeout 180000).

**Purpose**: the nutrition summary page — range presets, metric tiles, the
intake-vs-target chart, the macro split, and the paginated daily breakdown.

> **#208 redesign — behaviour change.** The custom from/to `DatePicker` range
> and the `Apply` button are **gone**. The range is now a three-option
> `SegmentedControl` (`7 days` / `30 days` / `90 days`, always ending today).
> The old "Totals" / "Daily average" cards and the distinct "Goal progress"
> panel are gone too — replaced by four `MetricTile`s (Avg intake, Avg
> protein, Days logged, Over-target days), a bar chart with a dashed target
> line, and a macro-split card. The daily breakdown is a CSS-grid list
> (`role="table"` / `"row"` / `"columnheader"`), not a `<table>`.

**Setup**:

- **Structure describe**: no setup; one test mocks `GET
  /api/v1/meals/nutrition-summary` → `[]` to force the empty state (there is no
  longer a guaranteed-empty past range to pick).
- **With-meal-data describe**: the `setup:` test imports a weekly plan via the
  import wizard. The "average-intake tile reflects the configured calorie goal"
  test configures goals via `/diet-planner/profile?section=goals` (raw
  `#dailyCalorieTarget` etc.).

**POM**: `pages/nutrition.page.ts`:

- `rangeGroup` — `getByRole('radiogroup', { name: /range/i })`
- `rangeOption('7'|'30'|'90')` — `rangeGroup.getByRole('radio', { name: '<n> days' })`
- `selectRange(n)` — clicks the option, awaits the `/nutrition-summary` GET
- `tableRows` — `getByRole('table').getByRole('row')`
- `getTableRowCount()` — row count minus the header row
- `pageSizeSelect` / `previousButton` / `nextButton` — the shared `Pagination`
- `expectEmptyState()` / `expectChartVisible()` / `expectMacroSplitVisible()`

## Tests — page structure

- **`range presets are visible and default to 7 days`** — `aria-checked` is
  `true` on the 7-day option, `false` on the others.
- **`switching to a longer range re-fetches the summary`** — `selectRange('30')`
  moves `aria-checked` to the 30-day option.
- **`an empty summary shows the empty state`** — with the mocked `[]` response,
  `/no meal data for the selected range/i` is visible.

## Tests — with meal data (serial)

- **`setup: import a weekly meal plan`** — imports `generateWeeklyPlan`, lands on
  `/diet-planner/calendar`. Cascades if it fails.
- **`metric tiles and the intake chart appear for the default 7-day range`** —
  `Avg intake` + `Days logged` tiles, `Intake vs Target` chart, `Macro split`
  card all visible.
- **`daily breakdown table has at least one row for the default range`** —
  `getTableRowCount() > 0`.
- **`a 90-day range … still renders`** — `selectRange('90')`, `Days logged` tile
  still visible.
- **`the average-intake tile reflects the configured calorie goal`** — after
  saving `dailyCalorieTarget: 2000`, the Avg-intake tile's hint sub-text is no
  longer the literal `kcal` (it switches to a signed delta vs the goal).
  Cross-spec state — this goal persists for the run.
- **pagination** — defaults to `25`; `setPageSize` updates the selector;
  Previous is disabled on page 1.

## Acceptance

The summary aggregates the selected preset range, reflects a configured calorie
goal, and paginates the daily breakdown.

## Gaps

- Custom date ranges (removed — presets only now)
- Specific aggregate values for known meal data
- The chart's dashed target line / bar heights
- Macro-split percentages
- CSV export, week-over-week comparison, column sorting
- Mobile layout
