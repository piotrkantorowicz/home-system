# E2E Scenarios

This document catalogues every scenario covered by the Playwright end-to-end suite at `src/E2ETests/`. Use it to find what's already tested before adding a new spec, to onboard reviewers, and to spot coverage gaps.

The suite has **83 tests across 13 specs**, plus one auth setup. It runs serially against a real backend, real frontend, and real Authentik (no mocks except for one explicit `route.fulfill` 404 case).

---

## Suite overview

| Concern | Choice |
|---|---|
| Framework | `@playwright/test` (Chromium only) |
| Test directory | `src/E2ETests/diet-planner/` |
| Page objects | `pages/` — POM per page, plus `profile-hub.helper.ts` for shared section navigation |
| Fixtures | `fixtures/auth.fixture.ts` — extends `test` with OIDC token refresh + entity-id tracking |
| Auth | Real Authentik (`http://localhost:9000`) — credentials from `TEST_USER_EMAIL` / `TEST_USER_PASSWORD` env vars (defaults: `E2eTestsUser` / `Password321!`) |
| Auth state | `playwright/.auth/user.json` — saved by `shared/auth.setup.ts`, refreshed per-test by the fixture |
| Workers | `1` — tests share backend state, must run serially |
| Web server | Auto-starts Vite via `npm --prefix ../Ui run dev`; reuses an existing server on `:5173` |
| Cleanup | `shared/global-teardown.ts` deletes tracked products + recipes + all meals after the run |
| Reporter | `html` (`playwright-report/`) + `list` |

### Auth fixture behaviour

Every test starts by trying to refresh the stored OIDC tokens via the refresh-token grant. On success it injects fresh tokens into `localStorage` before navigation (~200 ms). On failure (token revoked / Authentik restart), it falls back to a full interactive login through the Authentik UI and rewrites `playwright/.auth/user.json` for subsequent tests.

The fixture also subscribes to all responses and tracks IDs of POSTed products and recipes (URLs matching `/api/v1/products` and `/api/v1/recipes`) into `playwright/.test-data.json`. The global teardown reads this file and DELETEs each tracked entity through the API. Meals are not tracked individually — teardown deletes all meals for the test user unconditionally.

### Conventions

- **One POM per page**, named `<page>.page.ts`, exporting a class with locators in the constructor and methods for high-level actions.
- **Profile hub navigation** uses `gotoProfileSection(page, section)` from `pages/profile-hub.helper.ts`. All settings POMs (`profile`, `notification-preferences`, `meal-schedule`, `hydration-settings`) navigate via this helper.
- **Locators** prefer `getByRole + name` over `getByText`, and `#id` selectors on form fields when the React component renders a stable id.
- **Web-first assertions** (`expect(locator).toHaveX(...)`) — no manual polling, no `waitForTimeout`.
- **Test naming** is sentence case ("user can save their biometrics profile"), not Method_State_Expected.

---

## Backend state contract

Most specs are independent and can run in any order. A few share state and must run serially within their describe block:

| Spec | Reason |
|---|---|
| `import.spec.ts` | Each test creates products/recipes with `Date.now()`-suffixed names; serial avoids race conditions on the import wizard. |
| `meals.spec.ts` | All tests reuse a single weekly plan imported by the first `setup:` test. Later tests edit/delete meals from that plan. |
| `nutrition.spec.ts` (with-meal-data describe) | Same pattern — first test imports the plan, later tests assert nutrition aggregates. |
| `recipes.spec.ts` | Edit and delete tests reuse the recipe created by the first test. |

Cross-spec state: `goalCalories` configured by `nutrition.spec.ts › goal progress panel appears...` persists for the rest of the run (it's user-level data, not test-scoped). The weight-prediction POM tolerates this via a short-circuit when the calorie input is already at the requested value.

---

## Run instructions

### Prerequisites

```bash
# 1. Backend infra (Authentik + Postgres for diet-planner module)
cd infrastructure && docker compose --profile diet-planner up -d

# 2. Backend API
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apis/HomeSystem.REST

# 3. (Optional — Playwright auto-starts it) Vite frontend
cd src/ui && npm run dev
```

### Install + run

```bash
cd src/E2ETests
npm install
npm run install:browsers           # one-time: chromium with system deps

npm test                           # full suite (~4 min)
npm test diet-planner/profile      # one spec
npm run test:ui                    # interactive UI mode
npm run test:debug                 # step-through debugger
```

### Environment variables

Create `src/E2ETests/.env` (gitignored). The suite reads:

```env
PLAYWRIGHT_BASE_URL=http://localhost:5173
API_BASE_URL=http://localhost:5000
TEST_USER_EMAIL=E2eTestsUser
TEST_USER_PASSWORD=Password321!
```

---

## Specs

### `dashboard.spec.ts` — Dashboard

**Purpose**: smoke that the diet-planner dashboard renders its three stat cards and links into the right pages.

**Setup**: none (uses whatever counts the backend returns).

**Tests**:

- `all three stat cards are visible on load` — Products / Recipes / Calendar cards render
- `stat cards show numeric counts after data loads` — counts resolve to a number
- `each stat card links to its respective page` — clicking each card navigates to the right URL

**Acceptance**: the dashboard is reachable, the three module cards render, and their navigation contracts hold.

**Gaps**: the new goals CTA card (when no goals configured), the inline goal progress card (when goals exist), the WeightPredictionCard, and the welcome-empty-state messaging are all uncovered.

---

### `import.spec.ts` — Diet Plan Import (serial)

**Purpose**: end-to-end coverage of the multi-step import wizard — from JSON validation through to populated calendar entries.

**Setup**: each test generates its own JSON payload with `Date.now()`-suffixed names so they don't collide.

**Tests**:

- `user can load sample JSON and it populates the input` — Load Sample button works
- `entering invalid JSON blocks progression to step 2` — JSON parse error keeps the user on step 1
- `validation step shows success before reaching step 3` — server-side validation passes for a well-formed plan
- `full import wizard creates products, recipes and calendar entries` — import succeeds and redirects to `/diet-planner/calendar`
- `imported weekly plan meals are visible on the calendar` — bulk import of 4 meal types × 7 days produces 28 calendar entries

**Acceptance**: the wizard round-trips a JSON payload into actual products, recipes and meal entries that appear on the calendar.

**Gaps**: error handling when validation reports specific row-level failures, undo/cancel flow, large-payload behaviour, partial-failure recovery.

---

### `meals.spec.ts` — Calendar CRUD & Navigation (serial)

**Purpose**: meal CRUD operations on the calendar plus week navigation (previous / next / today).

**Setup**: first `setup:` test imports a weekly plan via the import wizard; subsequent tests reuse that data.

**Tests**:

- `setup: import a weekly meal plan` — seed
- `calendar shows current week with 7 day columns and a week header` — week renders
- `previous/next buttons navigate between weeks` — header text changes; Today snaps back
- `user can add a meal to a day slot` — add Snack on Monday
- `user can edit an existing meal to change servings` — open existing, change servings + note, save
- `user can delete a meal and it disappears from the calendar` — count of recipe-name links decreases by exactly one

**Acceptance**: the calendar's CRUD surface and week navigation work end-to-end.

**Gaps**: drag-and-drop between slots, bulk delete, recurring entries, calendar print/export, mobile (single-day) view.

---

### `pagination.spec.ts` — Pagination — Products list / Recipes list

**Purpose**: list pagination contract — page size selector and previous-button disabled state.

**Setup**: none.

**Tests** (per list, both lists tested identically):

- `page size selector is visible with the correct options` — `[10, 25, 50, 100]`
- `page size selector defaults to 25`
- `changing page size triggers a new API request` — verified via `waitForResponse`
- `Previous button is disabled when on the first page`

**Acceptance**: list pagination on Products and Recipes presents the standard four page sizes and disables the previous button at page 1.

**Gaps**: Next button enabled-when-more-pages, jump-to-last-page, page indicator text, URL-state preservation across reloads, calendar / nutrition pagination.

---

### `products.spec.ts` — Products

**Purpose**: products CRUD and search.

**Setup**: each test creates a uniquely-named product (`Date.now()` suffix); names are tracked for teardown.

**Tests**:

- `user can create a product with nutrition values` — calories/protein/carbs/fat all saved, product appears in list
- `user can search for products by name` — search box filters; either results or empty state visible
- `product form shows required-field error when name is empty` — client validation
- `user can edit an existing product` — edit calorie value; detail page reflects the new value
- `user can delete a product and it disappears from the list` — destructive action confirms

**Acceptance**: products CRUD surface works with form validation.

**Gaps**: macro percentage validation, server-side error display (e.g., 409 conflict on duplicate name), unit selector (g/ml/oz), import-from-recipe, bulk operations.

---

### `recipes.spec.ts` — Recipes CRUD (serial)

**Purpose**: recipe CRUD with at least one ingredient, plus reading nutrition-per-serving on the detail view.

**Setup**: first test creates an ingredient product and a recipe referencing it; subsequent tests reuse them.

**Tests**:

- `user can create a recipe with an ingredient` — recipe appears in list
- `user can view recipe details including the ingredient` — detail view shows nutrition per serving + ingredient name
- `user can edit a recipe to update the number of servings` — servings change to 4, detail reflects it
- `user can delete a recipe and it disappears from the list`

**Acceptance**: recipes CRUD works end-to-end with computed nutrition per serving.

**Gaps**: multi-ingredient recipes, ingredient amount/unit edge cases, instructions field, prep-time validation, dietary tags, recipe scaling math.

---

### `nutrition.spec.ts` — Nutrition Summary (mixed, partial serial)

**Purpose**: nutrition summary page — date range picker, totals, daily averages, goal progress, table pagination.

**Setup**:

- Structure tests (3): no setup; assert UI affordances regardless of data.
- With-meal-data tests (9, serial): first `setup:` imports a weekly plan; later tests assert aggregates against it. The "goal progress panel" test additionally configures goals through `/diet-planner/profile?section=goals`.

**Tests** (structure):

- `date range inputs are visible and default to the current week` — defaults match `startOfWeek(today, { weekStartsOn: 1 })`
- `selecting a past range with no meals shows the empty state`
- `Apply button is disabled when the from date is later than the to date`

**Tests** (with meal data):

- `setup: import a weekly meal plan` — seed
- `totals and daily average cards appear after applying the current week range`
- `daily breakdown table has at least one row for the current week`
- `switching to an empty date range shows the empty state`
- `goal progress panel appears when nutrition goals are configured` — configures goals via the profile hub, then asserts the panel is visible
- `pagination defaults to page size 25`
- `pagination: changing page size updates the selector value`
- `previous page button is disabled when on the first page`

**Acceptance**: the nutrition summary correctly aggregates a week of meals, exposes goal-progress when goals exist, and exposes a paginated daily breakdown.

**Gaps**: macro pie/bar visualisation, deficit/surplus calculation, exporting CSV, comparison against previous week, mobile layout.

---

### `profile.spec.ts` — Profile

**Purpose**: biometrics form CRUD on the profile hub's Body Stats section.

**Setup**: each test navigates to `/diet-planner/profile?section=body-stats` (the helper resolves to this URL).

**Tests**:

- `profile page loads and shows the form` — heading + key fields visible
- `user can save their biometrics profile` — fill all fields, save succeeds
- `saved profile values are pre-filled on next visit` — round-trip persistence
- `user can update an existing profile` — initial save then update; new value persists
- `save button is disabled when form is not dirty`
- `save button becomes enabled after editing a field`

**Acceptance**: the body-stats form persists biometrics correctly and exposes a sensible dirty/save UX.

**Gaps**: client-side validation for out-of-range height/weight, gender/activity-level dropdown coverage, date-of-birth keyboard input (the POM uses the calendar popover only), profile delete, profile export.

---

### `meal-schedule.spec.ts` — Meal Schedule

**Purpose**: meal-slot CRUD on the profile hub's Meal Schedule section (default slots: Breakfast, Lunch, Snack, Dinner; max 8, min 1).

**Setup**: navigates to `/diet-planner/profile?section=meal-schedule` via the helper.

**Tests**:

- `navigates to meal schedule page` — URL assertion against the hub pattern
- `shows default meal slots on first visit` — at least one slot rendered
- `save button is disabled when form is not dirty`
- `save button is enabled after editing a slot name`
- `can add a new slot` — slot count increases by one
- `add slot button is disabled when 8 slots exist`
- `can remove a slot` — slot count decreases (skipped when only one slot)
- `remove button is disabled when only one slot remains`
- `saving shows success message`

**Acceptance**: meal-slot CRUD is bounded by [1, 8] slots and the save flow shows feedback.

**Gaps**: time-input validation (out-of-range hours), duplicate-slot-name handling, drag-to-reorder slots.

---

### `hydration.spec.ts` — Hydration

**Purpose**: hydration page UI plus settings form CRUD on the profile hub. Split across two POMs: `HydrationPage` for `/diet-planner/hydration`, `HydrationSettingsPage` for `/diet-planner/profile?section=hydration`.

**Setup**: page-level tests navigate to `/diet-planner/hydration`; settings tests navigate via the profile-hub helper.

**Tests**:

- `hydration page loads and shows the heading`
- `progress bar is visible on the page`
- `settings form shows daily target and glass size fields` — settings POM
- `save settings button is visible` — settings POM
- `quick-add glass button is visible` — page-level
- `can update hydration settings` — settings POM, alternates values to guarantee dirty state

**Acceptance**: the hydration page renders progress and quick-add controls, and the settings form round-trips daily target + glass size.

**Gaps**: clicking the quick-add button to log intake (only checks visibility), deleting an intake entry, the intake history list, mobile layout, custom-amount entry.

---

### `notification-preferences.spec.ts` — Notification Preferences

**Purpose**: notification preferences CRUD on the profile hub's Notifications section. Lead-time and water-interval are now preset `<select>`s (`[5,10,15,30,60]` minutes for lead time; `[15,30,60,90,120]` for interval).

**Setup**: navigates to `/diet-planner/profile?section=notifications` via the helper.

**Tests**:

- `user can navigate to notification preferences page` — URL assertion + heading
- `page shows all notification sections` — meal / water / weekly summary / goal milestone groups visible
- `save button is disabled when form has not been changed`
- `save button becomes enabled after changing a setting`
- `user can save notification preferences` — toggle two checkboxes and save
- `meal lead time input is disabled when meal reminders are off`
- `water interval input is disabled when water reminders are off`
- `settings persist after saving and reloading page` — picks lead time `30` (a valid preset)

**Acceptance**: notification preferences round-trip correctly with appropriate enabled/disabled states for dependent inputs.

**Gaps**: actual delivery of notifications (out of scope for UI tests), localized lead-time labels, default-value display when no preferences exist server-side.

---

### `weight-prediction.spec.ts` — Weight prediction

**Purpose**: WeightPredictionCard on the dashboard — calorie input, prediction stats (BMR, TDEE, weekly change, BMI), and incomplete-profile fallback.

**Setup**: most tests set up a profile via `ProfilePage` first, then navigate to the dashboard. The card prefills its calorie input from the user's goals, so `enterCalories` short-circuits when the requested value already equals the prefilled value (React Query returns cached data, no new HTTP call).

**Tests**:

- `prediction card is visible on the dashboard`
- `shows prompt to enter calories when input is empty` — clears the input, asserts the prompt
- `displays BMR, TDEE and weekly change after entering calorie target` — full prediction visible
- `BMR and TDEE are positive numeric values`
- `shows weight loss trend when calorie target is below TDEE` — weekly change shows minus prefix
- `shows weight gain trend when calorie target is above TDEE` — plus prefix
- `shows estimated goal date when current and target weights differ`
- `prediction updates when calorie target is changed` — different inputs produce different weekly changes
- `returns 404 and shows incomplete-profile message when profile lacks required fields` — uses `page.route` to mock 404

**Acceptance**: the weight-prediction card calculates and displays correctly for a complete profile, and degrades gracefully when the backend can't compute a prediction.

**Gaps**: BMI category boundary checks (underweight / overweight / obese), goal-already-reached state, calorie input below `min=500` or above `max=10000`, prediction across multiple goal types.

---

### `theme.spec.ts` — Theme Toggle

**Purpose**: the theme toggle (now inside the user-menu dropdown — #108) cycles through modes and persists across reloads.

**Setup**: opens the user-menu dropdown once before clicking the toggle. The toggle is rendered as raw content inside the dropdown (not a `DropdownMenuItem`), so the dropdown stays open across clicks.

**Tests**:

- `clicking the theme toggle cycles through light and dark modes` — `<html>` class changes on each click
- `selected theme persists after a page reload` — cycle to dark, reload, assert dark class still present

**Acceptance**: the theme toggle works and persists user preference.

**Gaps**: system / auto theme mode (the toggle cycles light → dark → system per UI), per-component dark-mode rendering, language switcher (which lives next to the theme toggle but isn't tested).

---

## Known coverage gaps (cross-cutting)

Features that have no e2e coverage today:

- **Mobile sidebar drawer** (#95) — hamburger menu, drawer open/close, focus trap, escape to close.
- **Goals CTA card** (#110) on dashboard when no goals exist.
- **Hydration quick-add widget** (#111) on the Calendar tab — count increment/decrement, target reached state.
- **Calendar sub-views** (#111) — Calendar / Nutrition / Import tab navigation.
- **User profile dropdown** (#108) — language switcher, settings deep-links, logout.
- **Recipe search dropdown** (#112) — opacity / overlay behaviour.
- **Authentication failure paths** — invalid credentials, locked account, password reset flow (currently only the happy path is exercised by `auth.setup.ts`).
- **i18n parity** — every text-based selector hard-codes English; no Polish coverage.
- **Accessibility** — no keyboard-only flows, no screen-reader assertions, no axe checks.
- **Visual regression** — `playwright/visual` baselines do not exist.

Tracking issues for each cluster of gaps would belong in the same milestone as the relevant feature work.
