# notification-preferences.spec.ts — Notification Preferences

**Purpose**: notification preferences CRUD on the profile hub's Notifications section. Lead-time and water-interval are now preset `<select>`s (`[5, 10, 15, 30, 60]` minutes for lead time; `[15, 30, 60, 90, 120]` for interval).

**Setup**: navigates to `/diet-planner/profile?section=notifications` via the helper.

## Tests

- `user can navigate to notification preferences page` — URL assertion + heading
- `page shows all notification sections` — meal / water / weekly summary / goal milestone groups visible
- `save button is disabled when form has not been changed`
- `save button becomes enabled after changing a setting`
- `user can save notification preferences` — toggle two checkboxes and save
- `meal lead time input is disabled when meal reminders are off`
- `water interval input is disabled when water reminders are off`
- `settings persist after saving and reloading page` — picks lead time `30` (a valid preset)

## Acceptance

Notification preferences round-trip correctly with appropriate enabled/disabled states for dependent inputs.

## Gaps

- Actual delivery of notifications (out of scope for UI tests)
- Localized lead-time labels
- Default-value display when no preferences exist server-side
