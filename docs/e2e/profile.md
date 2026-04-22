# profile.spec.ts — Profile

**Purpose**: biometrics form CRUD on the profile hub's Body Stats section.

**Setup**: each test navigates to `/diet-planner/profile?section=body-stats` (the helper resolves to this URL).

## Tests

- `profile page loads and shows the form` — heading + key fields visible
- `user can save their biometrics profile` — fill all fields, save succeeds
- `saved profile values are pre-filled on next visit` — round-trip persistence
- `user can update an existing profile` — initial save then update; new value persists
- `save button is disabled when form is not dirty`
- `save button becomes enabled after editing a field`

## Acceptance

The body-stats form persists biometrics correctly and exposes a sensible dirty/save UX.

## Gaps

- Client-side validation for out-of-range height / weight
- Gender / activity-level dropdown coverage
- Date-of-birth keyboard input (the POM uses the calendar popover only)
- Profile delete
- Profile export
