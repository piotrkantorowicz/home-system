# notification-preferences.spec.ts — Notification Preferences

**Purpose**: notification preferences CRUD on the profile hub's Notifications section. The form has four toggle groups (meal reminder, water reminder, weekly summary, goal milestone) and two preset `<select>`s for lead time and water interval.

After the #114 settings refactor, lead-time and water-interval inputs became `<select>` dropdowns with preset values:

- Meal lead time: `[5, 10, 15, 30, 60]` minutes
- Water interval: `[15, 30, 60, 90, 120]` minutes

The POM uses `selectOption(String(minutes))` (not `fill`) to set them — using `fill` on a `<select>` throws.

**Setup**: each test creates a `NotificationPreferencesPage` and calls `goto()` → `gotoProfileSection(page, 'notifications')` → `/diet-planner/profile?section=notifications`.

**POM**: `pages/notification-preferences.page.ts`:

- All checkboxes located by id: `#mealReminderEnabled`, `#waterReminderEnabled`, `#weeklySummaryEnabled`, `#goalMilestoneAlertsEnabled`
- Selects also located by id: `#mealReminderLeadTimeMinutes`, `#waterReminderIntervalMinutes`
- `saveButton`: `getByRole('button', { name: /save preferences/i })`
- `successMessage`: `getByText(/saved successfully/i)`
- `save()` clicks Save and awaits `PUT /api/v1/notification-preferences`
- `toggleX(enable: boolean)` reads current state and clicks only when needed (idempotent)

## Tests

### `user can navigate to notification preferences page`

- **Given** the user calls `prefsPage.goto()`
- **When** navigation settles
- **Then** the URL matches `/\/diet-planner\/profile\?section=notifications/` and an h1 heading is visible
- **Notes**: heading text not asserted (i18n-agnostic).

### `page shows all notification sections`

- **Given** the user is on the notifications section
- **When** the page settles
- **Then** all four group labels are visible: `/meal reminder/i`, `/water reminder/i`, `/weekly nutrition summary/i`, `/goal milestone/i`
- **Notes**: uses `.first()` to handle duplicates (e.g., the heading text might appear in both the group label and a description).

### `save button is disabled when form has not been changed`

- **Given** the user is on the notifications section
- **When** a 500 ms wait lets the form load (either from `404 → defaults` or from existing prefs)
- **Then** the save button is disabled
- **Notes**: the explicit wait is a code smell but works around a race between async data load and the `isDirty` recalculation.

### `save button becomes enabled after changing a setting`

- **Given** the user is on the notifications section
- **When** `weeklySummaryCheckbox.click()` fires (forces a value change regardless of current state)
- **Then** the save button is enabled

### `user can save notification preferences`

- **Given** the user is on the notifications section
- **When** the user clicks `weeklySummaryCheckbox` and `goalMilestoneCheckbox` to guarantee dirtiness, then `save()` runs (clicks Save and awaits `PUT`)
- **Then** `successMessage` (`/saved successfully/i`) appears within 5 s
- **API**: `PUT /api/v1/notification-preferences`

### `meal lead time input is disabled when meal reminders are off`

- **Given** the user is on the notifications section with meal reminders ON (toggled on if not already)
- **When** `expectMealLeadTimeEnabled()` confirms baseline, then `toggleMealReminder(false)` turns reminders off
- **Then** `expectMealLeadTimeDisabled()` — the lead-time `<select>` is disabled
- **Notes**: tests the conditional-enabled relationship between the checkbox and the dependent select.

### `water interval input is disabled when water reminders are off`

- Symmetric to the meal lead-time test, but for `waterReminderCheckbox` ↔ `waterIntervalInput`.

### `settings persist after saving and reloading page`

- **Given** the user is on the notifications section with meal reminders ON
- **When** lead time is set to `30` (a valid preset), `weeklySummaryCheckbox` is clicked to guarantee dirtiness, the form is saved, and the page is re-navigated via `goto()`
- **Then** `mealLeadTimeInput.toHaveValue('30')` and the weekly-summary checkbox state matches what was saved
- **Notes**: `30` is chosen because it's a valid value in `MEAL_LEAD_TIME_OPTIONS = [5, 10, 15, 30, 60]`. Saving an invalid value would fail zod validation in the form.

## Acceptance

Notification preferences round-trip correctly. Dependent inputs (lead time, water interval) reflect the enabled state of their parent checkboxes. Save persists across reloads.

## Gaps

- Actual delivery of notifications (out of scope for UI tests — would need a notification service mock or real listener)
- Localized lead-time labels (`5 min` vs `5 minut`)
- Default-value display when no preferences exist server-side (`404 → defaults` path)
- Error toast when `PUT` fails (e.g., 500)
- Server-side validation (currently unreachable through the UI since the form is preset-`<select>`-bounded)
- Concurrent edits / stale-state conflicts
