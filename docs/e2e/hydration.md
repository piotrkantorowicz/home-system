# hydration.spec.ts — Hydration

**Purpose**: hydration page rendering, persisted water logging and removal, dashboard total, plus settings form on the profile hub.

> **#208 redesign** — the linear `role="progressbar"` is gone. The page shows a
> bottle-fill visual, now marked `role="meter"` + `aria-valuenow/min/max` +
> `aria-label` (i18n key `hydration.level_aria`) in `Hydration.tsx`. The
> glass-row (tap to add / tap the newest filled glass to remove) is shared with
> the dashboard's water card.

**POMs**:

- `pages/hydration.page.ts` — `/diet-planner/hydration` page-level locators
  (`levelMeter`, quick-add/custom controls, history and removal controls)
- `pages/hydration-settings.page.ts` — `/diet-planner/profile?section=hydration` settings form (`dailyTargetInput`, `glassSizeInput`, `saveSettingsButton`)
- `pages/dashboard.page.ts` — Water card total and progress

The settings POM uses the shared `gotoProfileSection(page, 'hydration')` helper from `profile-hub.helper.ts`.

## Tests

### `hydration page loads and shows the heading`

- **Given** the user navigates to `/diet-planner/hydration`
- **When** the page settles
- **Then** an h1 heading is visible (`getByRole('heading', { level: 1 })`)
- **Notes**: heading text is not asserted to keep i18n changes from breaking the test.

### `the water-level meter is visible on the page`

- **Given** the user is on `/diet-planner/hydration`
- **When** the page settles
- **Then** `getByRole('meter')` is visible
- **Notes**: the meter is always present regardless of fill level (`aria-valuenow` is 0 → 100).

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

### `water entries persist, can be removed, and update the dashboard`

- **Given** a 250 ml glass size and today's intake read from the real API as baseline
- **When** the user adds one glass and one 330 ml custom entry with a note
- **Then** history count increases by two and the note and combined total survive reload
- **When** the user removes the newest (custom) entry and reloads
- **Then** the note disappears, history count drops by one, and the total is baseline + 250 ml
- **And** the dashboard Water card shows that persisted total and matching progress value

## Acceptance

The hydration page renders progress, logs and removes entries through the real API, and the dashboard reflects persisted intake. The settings form saves daily target and glass size.

## Gaps

- Intake history across multiple days / day grouping
- Mobile layout (narrow viewport)
- Validation errors on settings form (out-of-range daily target / glass size)
- Goal-met visual state when current ≥ target
