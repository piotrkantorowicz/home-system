# meal-schedule.spec.ts — Meal Schedule

**Purpose**: meal-slot CRUD on the profile hub's Meal Schedule section (default slots: Breakfast, Lunch, Snack, Dinner; max 8, min 1).

**Setup**: navigates to `/diet-planner/profile?section=meal-schedule` via the helper.

## Tests

- `navigates to meal schedule page` — URL assertion against the hub pattern
- `shows default meal slots on first visit` — at least one slot rendered
- `save button is disabled when form is not dirty`
- `save button is enabled after editing a slot name`
- `can add a new slot` — slot count increases by one
- `add slot button is disabled when 8 slots exist`
- `can remove a slot` — slot count decreases (skipped when only one slot)
- `remove button is disabled when only one slot remains`
- `saving shows success message`

## Acceptance

Meal-slot CRUD is bounded by [1, 8] slots and the save flow shows feedback.

## Gaps

- Time-input validation (out-of-range hours)
- Duplicate-slot-name handling
- Drag-to-reorder slots
