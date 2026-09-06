# meals.spec.ts — Calendar CRUD & Navigation

`test.describe.configure({ mode: 'serial', timeout: 180000 })` — all tests share
state from the `setup:` import.

**Purpose**: meal CRUD on the calendar week grid plus week navigation.

> **#208 redesign — the week view was rebuilt as `WeekGrid`.** It is now a
> single ARIA grid: `role="grid"`, day headers `role="columnheader"`
> (aria-label = short weekday, e.g. `Mon`), slot labels `role="rowheader"`,
> day/slot cells `role="gridcell"` (aria-label = `<weekday> <slot>`, e.g.
> `Mon Snack`). An empty cell holds a dashed **"Add Meal"** `<button>`. A
> scheduled meal is a **chip `<button>`** (accessible name `<recipe> <kcal>
> kcal`) that opens a **dropdown menu** (Mark done / Record actual / Revert /
> Edit / Delete) — it is no longer a link, and there is no hover-reveal icon row.

**Setup**: the `setup:` test imports `generateWeeklyPlan(new Date())`. Later
tests use `planData.recipeNames[1]` (Chicken Rice).

**POM**: `pages/calendar.page.ts`:

- `cell(weekday, slot)` — `getByRole('gridcell', { name: '<weekday> <slot>' })`
- `mealChips(recipeName)` — `getByRole('button', { name: recipeName })` (page-wide)
- `clickAddMeal(weekday, slot)` — clicks the cell's `Add Meal` button, waits for
  the MealForm dialog
- `fillMealForm` / `submitMealForm` — unchanged (recipe search + suggestion
  button, servings, notes; submit `Add Meal` / `Save Changes`)
- `openEditMeal(name)` — clicks the chip, then the portaled `Edit` menuitem
- `deleteMeal(name)` — clicks the chip, then the `Delete` menuitem, then confirms
  in the `Delete Meal` dialog
- `expectMealInDay(weekday, name)` — a `<weekday> …` gridcell contains the chip

## Tests

- **`setup: import a weekly meal plan`** — cascades if it fails.
- **`calendar shows current week with 7 day columns and a week header`** — the
  `/week of/i` header + all 7 `role="columnheader"` weekday cells are visible.
- **`previous/next buttons navigate between weeks`** — a 4-step nav sequence
  (Next → Previous → Next → Today) with header-text assertions.
- **`user can add a meal to a day slot`** — `clickAddMeal('Mon', 'Snack')`,
  `fillMealForm(recipe, 2.5)`, submit → chip visible in a Monday cell.
- **`user can edit an existing meal to change servings`** — open the chip's Edit,
  update servings + notes, submit → chip still visible (weak assertion, no
  value read-back).
- **`user can delete a meal and it disappears from the calendar`** — count
  `mealChips` before, `deleteMeal`, assert count is `before - 1`.

## Acceptance

Calendar CRUD and week navigation work end-to-end against the WeekGrid.

## Gaps

- Drag-and-drop between slots (blocked — no new deps per project overrides)
- The chip dropdown's other actions (Mark done / Record actual / Revert / bulk-complete)
- Notes round-trip verification
- Servings value read-back after edit
- Day view (`view: 'day'`) — only the week grid is tested
- Mobile layout, keyboard nav
