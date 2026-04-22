# hydration.spec.ts — Hydration

**Purpose**: hydration page UI plus settings form CRUD on the profile hub. Split across two POMs: `HydrationPage` for `/diet-planner/hydration`, `HydrationSettingsPage` for `/diet-planner/profile?section=hydration`.

**Setup**: page-level tests navigate to `/diet-planner/hydration`; settings tests navigate via the profile-hub helper.

## Tests

- `hydration page loads and shows the heading`
- `progress bar is visible on the page`
- `settings form shows daily target and glass size fields` — settings POM
- `save settings button is visible` — settings POM
- `quick-add glass button is visible` — page-level
- `can update hydration settings` — settings POM, alternates values to guarantee dirty state

## Acceptance

The hydration page renders progress and quick-add controls, and the settings form round-trips daily target + glass size.

## Gaps

- Clicking the quick-add button to log intake (only checks visibility)
- Deleting an intake entry
- The intake history list
- Mobile layout
- Custom-amount entry
