# import.spec.ts — Diet Plan Import

`test.describe.configure({ mode: 'serial', timeout: 120000 })`

**Purpose**: end-to-end coverage of the multi-step import wizard — from JSON validation through to populated calendar entries.

**Setup**: each test generates its own JSON payload with `Date.now()`-suffixed names so they don't collide.

## Tests

- `user can load sample JSON and it populates the input` — Load Sample button works
- `entering invalid JSON blocks progression to step 2` — JSON parse error keeps the user on step 1
- `validation step shows success before reaching step 3` — server-side validation passes for a well-formed plan
- `full import wizard creates products, recipes and calendar entries` — import succeeds and redirects to `/diet-planner/calendar`
- `imported weekly plan meals are visible on the calendar` — bulk import of 4 meal types × 7 days produces 28 calendar entries

## Acceptance

The wizard round-trips a JSON payload into actual products, recipes and meal entries that appear on the calendar.

## Gaps

- Error handling when validation reports specific row-level failures
- Undo / cancel flow
- Large-payload behaviour
- Partial-failure recovery
