# profile.spec.ts — Profile

**Purpose**: biometrics form CRUD on the profile hub's Body Stats section — initial save, persistence across navigation, in-place updates, and dirty-state-driven save button enablement.

**Setup**: each test creates a fresh `ProfilePage` and calls `goto()`, which navigates to `/diet-planner/profile?section=body-stats` and waits for the **Save Profile** button (the form only mounts once the profile query has resolved). No backend seeding — tests assume the user already exists and operate on the user's profile entity.

**POM**: `pages/profile.page.ts` exposes:

- `dateOfBirthInput`: `getByTestId('date-of-birth-picker')` — Radix popover-backed date picker
- `genderSelect`: `getByLabel(/^gender$/i)`
- `heightInput`: `getByLabel(/height/i)`
- `currentWeightInput`: `getByLabel(/current weight/i)`
- `targetWeightInput`: `getByLabel(/target weight/i)`
- `activityLevelSelect`: `getByLabel(/activity level/i)`
- `saveButton`: `getByRole('button', { name: /save profile/i })`
- `successMessage`: `getByText(/profile saved successfully/i)`
- `fieldError`: `getByRole('alert')` — `BodyStatsForm`'s per-field error `<p>`s now carry `role="alert"`

The POM's `fillForm()` method intentionally fills the date-of-birth field LAST. Interacting with form controls can trigger TanStack Query's `refetchOnWindowFocus`, which fires `useEffect → reset()` and clears all values. Setting the date last minimises the window between filling it and clicking Save.

## Tests

### `profile page loads and shows the form`

- **Given** the user is on `/diet-planner/profile?section=body-stats`
- **When** the Body Stats form has mounted (its **Save Profile** button is on screen)
- **Then** the heading matches `/profile.*settings|profile/i` (the i18n value is `Profile & Settings`), and the save button, height input, and gender select are all visible
- **Notes**: the heading regex is permissive to tolerate i18n changes; the field assertions are the actual smoke test that the body-stats form mounted.

### `user can save their biometrics profile`

- **Given** the user is on the profile page
- **When** the form is filled with `gender: Male`, `heightCm: 180`, `currentWeightKg: 80`, `targetWeightKg: 75`, `activityLevel: ModeratelyActive` and `Save profile` is clicked
- **Then** `successMessage` (`profile saved successfully`) appears within 10 s
- **API**: `PUT` to the profile endpoint (handled by the React Query mutation)

### `saved profile values are pre-filled on next visit`

- **Given** the user has saved a profile with `heightCm: 175`, `currentWeightKg: 72`, `targetWeightKg: 68`, `activityLevel: LightlyActive`
- **When** the user navigates away to `/diet-planner` then back to `/diet-planner/profile?section=body-stats`
- **Then** the form fields are pre-filled with the saved values (verified via `expectFieldValue('heightCm', 175)`, etc.)
- **Notes**: round-trips through both the API write and the API read; the navigate-away-and-back step proves React Query refetches on remount, not just an in-memory cache.

### `user can update an existing profile`

- **Given** an initial save with `heightCm: 170`, `currentWeightKg: 85`, `targetWeightKg: 80`
- **When** a second `fillForm` updates `currentWeightKg` to 83, `targetWeightKg` to 78, `activityLevel` to `VeryActive`, and Save is clicked
- **Then** `currentWeightKg` reads `83` after the second save
- **Notes**: doesn't navigate away between updates; proves the same form can transition from create to update without a remount.

### `save button is disabled when form is not dirty`

- **Given** the user lands on the profile page (form values come from the existing profile)
- **When** no field is touched
- **Then** the `Save profile` button is disabled
- **Notes**: relies on react-hook-form's `isDirty` state; if the form initialises with default values that differ from the loaded values, this test will fail with a false positive.

### `save button becomes enabled after editing a field`

- **Given** the user is on the profile page with the save button disabled
- **When** the height input is filled with `182`
- **Then** the save button is enabled
- **Notes**: a focused, fast assertion of the dirty-state contract — no network calls.

### `out-of-range body stats show field errors and do not save`

- **Given** a known-good baseline (`heightCm: 180`, `currentWeightKg: 80`) is saved
- **When** height is set to `301` (above the 300 cm bound) and Save is clicked, then separately weight is set to `601` (above the 600 kg bound)
- **Then** `fieldError` (`role="alert"`) appears and `successMessage` stays hidden each time; a reload confirms the baseline values, not the rejected ones, persisted
- **Notes**: purely client-side (Zod) — no request is sent for an invalid submission, so the reload is what proves nothing saved, not a mocked response.

### `failed profile save shows error feedback without persisting`

- **Given** a known-good baseline (`heightCm: 190`) is saved
- **When** the profile `POST`/`PUT` is route-mocked to `500` and height is changed to `195`, then Save is clicked
- **Then** the "Failed to save profile" toast appears; after unrouting and reloading, the height reads `190` — the mocked failure never reached the real backend
- **API**: route-mocked (`page.route('**/api/v1/profile', …)`) — the real backend cannot produce a 500 on demand

## Acceptance

The body-stats form persists biometrics correctly, surfaces a sensible dirty/save UX, round-trips through navigation, rejects out-of-range values without saving, and shows error feedback on a failed save without corrupting persisted data.

## Gaps

- Gender / activity-level dropdown coverage beyond the two values used by the suite
- Date-of-birth keyboard input (the POM uses the calendar popover only; year-jump UX is covered, but typed entry is not)
- Server-side validation errors (e.g., 400 from the API) — only a 500 is exercised
