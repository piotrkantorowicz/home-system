# meal-schedule.spec.ts — Meal Schedule

**Purpose**: meal-slot CRUD on the profile hub's Meal Schedule section. Slots are bounded `[1, 8]` — the form starts with default slots (Breakfast, Lunch, Snack, Dinner), the Add button disables at 8, and the Remove button disables at 1.

**Setup**: each test creates a `MealSchedulePage` and calls `goto()`, which uses `gotoProfileSection(page, 'meal-schedule')` → `/diet-planner/profile?section=meal-schedule`. No backend seeding; the form populates from the user's saved schedule (or the React component's defaults).

**POM**: `pages/meal-schedule.page.ts`:

- `addSlotButton`: `getByRole('button', { name: /add slot/i })`
- `saveButton`: `getByRole('button', { name: /save schedule/i })`
- `successMessage`: `getByText(/saved successfully/i)`
- `slotNameInput(i)`: `page.locator('#slot-${i}-name')` (id-based)
- `slotTimeInput(i)`: `page.locator('#slot-${i}-time')`
- `slotRemoveButton(i)`: `getByRole('button', { name: /remove slot/i }).nth(i)`
- `slotCount()`: counts all `input[id^="slot-"][id$="-name"]` elements

## Tests

### `navigates to meal schedule page`

- **Given** the user calls `schedulePage.goto()`
- **When** the navigation settles
- **Then** the URL matches `/\/diet-planner\/profile\?section=meal-schedule/`
- **Notes**: the regex assertion (not exact equality) tolerates additional query params if any are added later.

### `shows default meal slots on first visit`

- **Given** the user is on the meal-schedule section
- **When** the page settles
- **Then** `slotCount()` is at least 1, and the first slot's name and time inputs are visible
- **Notes**: the lower-bound assertion (`toBeGreaterThanOrEqual(1)`) tolerates whatever the user's saved schedule looks like — the strict assertion is just that at least one slot renders.

### `save button is disabled when form is not dirty`

- **Given** the user lands on the meal-schedule section
- **When** no field is touched
- **Then** the save button is disabled
- **Notes**: react-hook-form `isDirty` contract.

### `save button is enabled after editing a slot name`

- **Given** the user is on the meal-schedule section
- **When** the first slot's name input is filled with `Updated Breakfast`
- **Then** the save button is enabled
- **Notes**: dirty-state UX assertion — no API call.

### `can add a new slot`

- **Given** the current slot count is `n`
- **When** `addSlot('Late Snack', '21:00')` clicks the Add button, fills the new slot's name and time
- **Then** `slotCount()` is `n + 1`
- **Notes**: doesn't save — proves the UI affordance, not persistence.

### `add slot button is disabled when 8 slots exist`

- **Given** the user is on the meal-schedule section
- **When** the Add button is clicked in a `while` loop until `slotCount() === 8`
- **Then** the Add button becomes disabled
- **Notes**: relies on the bounded loop terminating; if the form caps at fewer than 8 the loop stalls until Playwright's action timeout.

### `can remove a slot`

- **Given** the current slot count is `n` (skipped if `n <= 1`)
- **When** the first slot's Remove button is clicked
- **Then** `slotCount()` is `n - 1`
- **Notes**: only runs the assertion when there's more than one slot; otherwise the test passes trivially without exercising remove.

### `remove button is disabled when only one slot remains`

- **Given** the user is on the meal-schedule section
- **When** Remove is clicked in a `while` loop until only one slot remains
- **Then** the remaining slot's Remove button is disabled
- **Notes**: proves the lower bound `[1, 8]` is enforced via UI state, not just on save.

### `saving shows success message`

- **Given** the form is loaded
- **When** the first slot's name is toggled (`Morning Meal` ↔ `Breakfast`) to guarantee dirtiness, then Save is clicked
- **Then** `successMessage` (`/saved successfully/i`) appears within 10 s
- **API**: `PUT` to the meal-schedule endpoint
- **Notes**: alternating values defends against shared backend state — the previous run might have left either value as the saved name.

## Acceptance

Meal-slot CRUD is bounded by `[1, 8]` slots, the dirty-state UX correctly enables/disables Save, and the save flow shows feedback.

## Gaps

- Time-input validation (out-of-range hours, invalid HH:MM)
- Duplicate-slot-name handling (server-side validation)
- Empty form submission (zero slots — should be impossible per the lower bound)
