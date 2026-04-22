# hydration.spec.ts — Hydration

**Purpose**: the hydration page UI (heading, progress bar, quick-add control) plus settings form CRUD on the profile hub. Page-level concerns and settings concerns live on different routes after #114, so the spec uses two POMs.

**POMs**:

- `pages/hydration.page.ts` — `/diet-planner/hydration` page-level locators (heading, progress bar)
- `pages/hydration-settings.page.ts` — `/diet-planner/profile?section=hydration` settings form (`dailyTargetInput`, `glassSizeInput`, `saveSettingsButton`)

The settings POM uses the shared `gotoProfileSection(page, 'hydration')` helper from `profile-hub.helper.ts`.

## Tests

### `hydration page loads and shows the heading`

- **Given** the user navigates to `/diet-planner/hydration`
- **When** the page settles
- **Then** an h1 heading is visible (`getByRole('heading', { level: 1 })`)
- **Notes**: heading text is not asserted to keep i18n changes from breaking the test.

### `progress bar is visible on the page`

- **Given** the user is on `/diet-planner/hydration`
- **When** the page settles
- **Then** the progress bar wrapper (`div:has(> [role="progressbar"])`) is visible
- **Notes**: targets the wrapper rather than the progressbar element directly so the test passes whether the bar is at 0% or filled.

### `settings form shows daily target and glass size fields`

- **Given** the user navigates to `/diet-planner/profile?section=hydration`
- **When** the settings form mounts
- **Then** `dailyTargetInput` (label `/daily.*target/i`) and `glassSizeInput` (label `/glass size/i`) are both visible
- **Notes**: uses the new settings POM — proves the settings form renders inside the profile hub.

### `save settings button is visible`

- **Given** the user is on the hydration settings section
- **When** the form mounts
- **Then** `saveSettingsButton` (`getByRole('button', { name: /save settings/i })`) is visible

### `quick-add glass button is visible`

- **Given** the user is on `/diet-planner/hydration`
- **When** the page settles
- **Then** at least one button matching `/\+.*ml|glass/i` is visible
- **Notes**: the regex tolerates either localized "+250ml" or generic "Add glass" labels. The test only checks visibility; clicking the button is not exercised.

### `can update hydration settings`

- **Given** the user is on `/diet-planner/profile?section=hydration` with current values loaded
- **When** the user reads the current daily target (`3000` → set to `2500`, otherwise `3000`) and current glass size (`300` → set to `250`, otherwise `300`), then `updateSettings` fills both fields and clicks Save
- **Then** after save the daily target input is still visible (form remains mounted, not unmounted on save)
- **API**: `PUT` to the hydration config endpoint (awaited implicitly via React Query mutation)
- **Notes**: alternating values guarantees the form is always dirty regardless of prior run state — the suite is serial and shares backend state, so a fixed value would no-op on the second run. The post-save visibility check is a weak assertion; the strong check is implicit (no error toast, no exception).

## Acceptance

The hydration page renders progress and quick-add controls, and the settings form round-trips daily target + glass size through the API.

## Gaps

- Clicking the quick-add button to actually log intake (only checks visibility)
- Logging custom-amount intake via the secondary input
- Deleting an intake entry
- The intake history list / day grouping
- Mobile layout (narrow viewport)
- Validation errors on settings form (out-of-range daily target / glass size)
- Goal-met visual state when current ≥ target
