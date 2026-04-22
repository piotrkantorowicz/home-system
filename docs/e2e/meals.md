# meals.spec.ts — Calendar CRUD & Navigation

`test.describe.configure({ mode: 'serial', timeout: 180000 })`

**Purpose**: meal CRUD operations on the calendar plus week navigation (previous / next / today).

**Setup**: first `setup:` test imports a weekly plan via the import wizard; subsequent tests reuse that data.

## Tests

- `setup: import a weekly meal plan` — seed
- `calendar shows current week with 7 day columns and a week header` — week renders
- `previous/next buttons navigate between weeks` — header text changes; Today snaps back
- `user can add a meal to a day slot` — add Snack on Monday
- `user can edit an existing meal to change servings` — open existing, change servings + note, save
- `user can delete a meal and it disappears from the calendar` — count of recipe-name links decreases by exactly one

## Acceptance

The calendar's CRUD surface and week navigation work end-to-end.

## Gaps

- Drag-and-drop between slots
- Bulk delete
- Recurring entries
- Calendar print / export
- Mobile (single-day) view
