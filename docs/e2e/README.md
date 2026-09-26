# E2E Tests

The Playwright end-to-end suite at `e2e/` covers **89 tests across 15 spec
files**, plus the auth setup (4 worker logins). It runs against a real backend,
real frontend, and real Authentik; the two `notifications/` specs are fully
route-mocked, and a few diet-planner tests mock one endpoint to force a
deterministic branch (see per-spec docs).

> **Audited against the #208 UI redesign (2026-09).** The redesign moved,
> restructured, or removed a lot of what the suite targeted — see
> [Redesign impact & findings](#redesign-impact--findings) below. All specs
> and POMs in this doc reflect the post-redesign app.

This directory is the human-readable reference. Each spec has its own page below; this README is the hub for cross-cutting concerns and discovery.

---

## Specs

| Spec | What it covers | Tests |
|---|---|---|
| [dashboard](dashboard.md) | Today hero + quick actions on the redesigned dashboard | 5 |
| [import](import.md) | 2-step import wizard end-to-end | 5 |
| [meals](meals.md) | WeekGrid calendar CRUD and week navigation | 6 |
| [pagination](pagination.md) | List pagination on Products and Recipes | 8 |
| [products](products.md) | Products CRUD and search | 5 |
| [recipes](recipes.md) | Recipes CRUD with ingredients | 4 |
| [nutrition](nutrition.md) | Nutrition summary — range presets, tiles, chart, table | 12 |
| [profile](profile.md) | Biometrics form on the profile hub | 6 |
| [meal-schedule](meal-schedule.md) | Meal slots on the profile hub | 9 |
| [hydration](hydration.md) | Hydration page + settings | 6 |
| [notification-preferences](notification-preferences.md) | `diet-reminder-settings.spec.ts` — reminder prefs on the profile hub (now `serial`) | 8 |
| [weight-prediction](weight-prediction.md) | Read-only "Energy model" card on Profile → Overview | 6 |
| [theme](theme.md) | Theme toggle in the user-menu dropdown | 2 |
| [notifications](notifications.md) | `notifications/` module — channel prefs + inbox (route-mocked) | 4 |

---

## Suite overview

| Concern | Choice |
|---|---|
| Framework | `@playwright/test` (Chromium only) |
| Test directory | `e2e/diet-planner/` + `e2e/notifications/` (`testDir: '.'`) |
| Page objects | `pages/` — POM per page, plus `profile-hub.helper.ts` for shared section navigation |
| Fixtures | `fixtures/auth.fixture.ts` — extends `test` with OIDC token refresh and per-worker `storageState` resolution |
| Auth | Real Authentik (`http://localhost:9000`) — one user per worker (`E2eWorker0`..`E2eWorker3`), password sourced from `TEST_USER_PASSWORD` in `e2e/.env` (must match `E2E_USER_PASSWORD` in `infrastructure/.env`). Per-worker overrides available via `TEST_USER_EMAIL_<n>` / `TEST_USER_PASSWORD_<n>` |
| Auth state | `playwright/.auth/user-${workerIndex}.json` — one file per worker, saved by `shared/auth.setup.ts`, refreshed per-test by the fixture |
| Household seed | `shared/household-seed.ts` — after each worker logs in, `auth.setup.ts` ensures the user belongs to a household (`GET /api/households/me` → on 404 `POST /api/persons/me/sync` + `POST /api/households`). Without it `HouseholdRequired` redirects every module route to `/household`. Idempotent; the household is not purged by teardown |
| Workers | `4` — each worker owns a distinct Authentik user so tests run in parallel without cross-worker data contention |
| Web server | Auto-starts Vite via `npm --prefix ../src/ui run dev`; reuses an existing server on `:5173` |
| Cleanup | `shared/global-teardown.ts` calls `DELETE /api/v1/test-support/purge-my-data` once per worker (in parallel) — a single hard-purge of all owned rows across every DietPlanner aggregate |
| Reporter | `html` (`playwright-report/`) + `list` |

---

## Auth fixture behaviour

Each worker runs as a dedicated Authentik user. `shared/auth.setup.ts` logs in all four users sequentially at the start of the suite and writes one storage-state file per worker (`playwright/.auth/user-0.json` … `user-3.json`). The auth fixture picks the right file via `testInfo.workerIndex` and overrides the default `storageState` fixture accordingly.

Right after each login the setup project seeds a household for that user (see `shared/household-seed.ts`). The SPA's `HouseholdRequired` gate redirects household-less users to the onboarding page, so this must happen before any spec navigates. The household persists across runs — teardown only purges DietPlanner data.

Before each test the fixture tries to refresh the stored OIDC tokens via the refresh-token grant. On success it injects fresh tokens into `localStorage` before navigation (~200 ms). On failure (token revoked / Authentik restart), it falls back to a full interactive login through the Authentik UI and rewrites the worker's auth file for subsequent tests.

### Scaling the worker count

To run more (or fewer) workers:

1. Add matching `E2eWorker<n>` entries to `infrastructure/authentik/blueprints/home-system.yaml`, bounce Authentik (`docker compose down && docker compose up -d`).
2. Bump the `WORKER_COUNT` constant in `e2e/shared/auth.setup.ts`.
3. Set `workers: <n>` in `e2e/playwright.config.ts`.

The auth fixture and teardown infer the count from the Playwright runtime, so no other code needs updating.

---

## Backend requirement: test-support endpoint must be enabled

Cleanup relies on `DELETE /api/v1/test-support/purge-my-data`, which hard-deletes every DietPlanner row owned by the caller. The endpoint is only registered when **one of** the following is true:

- `ASPNETCORE_ENVIRONMENT=Development` (the default for local runs).
- `E2ETestSupport__Enabled=true` (environment variable) or the equivalent `"E2ETestSupport": { "Enabled": true }` in `appsettings.*.json`.

Running the backend in `Production` or `Staging` without the override returns **404** on that route — both a safety guard and a signal to set the flag before a CI run. Teardown logs a warning when it hits 404 so misconfiguration is obvious in the test output.

---

## Conventions

- **One POM per page**, named `<page>.page.ts`, exporting a class with locators in the constructor and methods for high-level actions.
- **Profile hub navigation** uses `gotoProfileSection(page, section)` from `pages/profile-hub.helper.ts`. All settings POMs (`profile`, `notification-preferences`, `meal-schedule`, `hydration-settings`) navigate via this helper.
- **Locators** prefer `getByRole + name` over `getByText`, and `#id` selectors on form fields when the React component renders a stable id.
- **Web-first assertions** (`expect(locator).toHaveX(...)`) — no manual polling, no `waitForTimeout`.
- **Test naming** is sentence case ("user can save their biometrics profile"), not Method_State_Expected.
- **Per-test seeding** — when a test needs a specific backend state (profile exists, notification preferences set, meal schedule configured), use the helpers in `e2e/diet-planner/utils/seed.ts` (`seedProfile`, `seedNotificationPreferences`, `seedMealSchedule`) rather than driving the UI. Each call hits the API directly with the current user's token and is idempotent. Tests must not rely on state leftover from earlier runs — the teardown purge wipes everything.

---

## Backend state contract

Most specs are independent and can run in any order. A few share state and must run serially within their describe block:

| Spec | Reason |
|---|---|
| [import](import.md) | Each test creates products/recipes with `Date.now()`-suffixed names; serial avoids race conditions on the import wizard. |
| [meals](meals.md) | All tests reuse a single weekly plan imported by the first `setup:` test. Later tests edit/delete meals from that plan. |
| [nutrition](nutrition.md) (with-meal-data describe) | Same pattern — first test imports the plan, later tests assert nutrition aggregates. |
| [recipes](recipes.md) | Edit and delete tests reuse the recipe created by the first test. |
| [notification-preferences](notification-preferences.md) (`diet-reminder-settings.spec.ts`) | Every test writes the same per-user reminder-settings record — made `serial` during the audit. |
| [weight-prediction](weight-prediction.md) | Every test writes the same per-user profile + goals record — `serial` + per-test profile seeding. |

Cross-spec state: a `dailyCalorieTarget` goal configured by `nutrition.spec.ts`
(and by `weight-prediction.spec.ts`) persists for the rest of the run (it's
user-level data). `weight-prediction.spec.ts`'s empty-state test route-mocks
`GET /api/v1/goals → null` to stay deterministic despite this.

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
cd e2e
npm install
npm run install:browsers           # one-time: chromium with system deps

npm test                           # full suite (~4 min)
npm test diet-planner/profile      # one spec
npm run test:ui                    # interactive UI mode
npm run test:debug                 # step-through debugger
```

### Environment variables

Two files are involved — both gitignored, both derived from their `.env.example` sibling. The same test-user password must appear in both so Authentik provisions the accounts with the value Playwright logs in with.

**`infrastructure/.env`** (read by Docker Compose when bringing Authentik up):

```env
E2E_USER_PASSWORD=<shared password for E2eWorker0..E2eWorker3>
```

**`e2e/.env`** (auto-loaded by `playwright.config.ts` via `dotenv`):

```env
PLAYWRIGHT_BASE_URL=http://localhost:5173
API_BASE_URL=http://localhost:5050
TEST_USER_PASSWORD=<must match E2E_USER_PASSWORD above>
# Optional per-worker overrides:
# TEST_USER_EMAIL_<n>=E2eWorker<n>
# TEST_USER_PASSWORD_<n>=<per-worker password>
```

`TEST_USER_PASSWORD` (or every `TEST_USER_PASSWORD_<n>`) is **required** — the auth helpers throw a clear error rather than falling back to a committed default.

---

## Nightly CI

`.github/workflows/e2e-nightly.yml` runs the whole suite against the real stack on
`ubuntu-latest` every night at **03:00 UTC**, on demand (`workflow_dispatch` — *Actions
→ E2E Nightly → Run workflow*, any branch), and on every PR targeting `main`. The PR run
is **report-only** — it is not in required status checks yet, while the flake rate is
being watched (issue #379); a red check does not block merge. A PR from a fork skips the
job entirely (no `TEST_USER_PASSWORD` secret). `scripts/verify.sh` still only
type-checks `e2e/` — it does not run the suite. Run the suite locally before shipping a
UI flow.

What the job does, in order:

1. Writes `infrastructure/.env` with **generated** values for everything the compose
   file interpolates (`AUTHENTIK_SECRET_KEY`, DB / Redis passwords, bootstrap account) —
   the runner is throwaway, so nothing but the E2E password needs to be a real secret.
2. `docker compose --profile diet-planner --profile notifications --profile household up -d --wait`
   in `infrastructure/`, then polls `/-/health/ready/` and the
   `home-system` OIDC discovery document (bounded, 5 min each). The discovery document
   only exists once the worker has applied `authentik/blueprints/home-system.yaml`, and
   the blueprint applies atomically, so the provider, the application and
   `E2eWorker0..3` are all present at that point.
3. Builds the host and starts it with `ASPNETCORE_ENVIRONMENT=Development` (EF / DbUp
   auto-migrate, the `test-support` purge route is mapped) and the generated passwords
   injected as `ConnectionStrings__*`, then waits for `/health`.
4. `npm ci` in `src/ui` and `e2e`, `playwright install --with-deps chromium`,
   `npx playwright test --project=chromium` with `CI=true` — Playwright starts Vite itself
   (`reuseExistingServer` is off on CI), retries twice, and `test.only` is an error.
5. On failure: uploads `e2e/playwright-report` + `e2e/test-results` (artifact
   `playwright-report`) and `backend.log` + `docker compose logs` (artifact `stack-logs`),
   14-day retention. **These artifacts can contain the e2e password** — a retry trace
   records the login form POST, and the stack logs echo whatever the containers print —
   so `TEST_USER_PASSWORD` must be a throwaway value used only by `E2eWorker0..3`, never
   a password reused anywhere else. Artifacts are visible to everyone with read access
   to the repository.

### Failure notification

The workflow keeps **one tracking issue** — title `Nightly e2e failed`, labels `bug` +
`ci` + `e2e` (the lookup keys on the title plus `ci` + `e2e`) — and only for runs on
`main`:

- a failed run opens it, or comments the run URL on it if it is already open;
- the next green run comments "green again" and closes it.

A `workflow_dispatch` or `pull_request` run never touches issues — look at the run
itself and its artifacts. Scheduled / dispatch runs share one queue and never overlap
(nothing is cancelled), so a manual run during the nightly simply queues. A PR run gets
its own queue keyed by PR number, and a new push cancels the one it supersedes.

### Repository secrets (owner action)

| Secret | Value | Used for |
|---|---|---|
| `TEST_USER_PASSWORD` | Any string without `$` (compose interpolates `.env`); alphanumeric is safest. It does **not** have to match your local `infrastructure/.env`. | Written to `infrastructure/.env` as `E2E_USER_PASSWORD` so the Authentik blueprint provisions `E2eWorker0..3` with it, and passed to Playwright as `TEST_USER_PASSWORD`. |

The job fails fast with a `::error::` annotation when the secret is missing. No other
secret is required — the Authentik bootstrap password, secret key and every database
password are generated per run and never leave the runner.

---

## Redesign impact & findings

What the #208 audit changed, and what it surfaced.

### Behaviour changes the redesign shipped (tests were rewritten to match)

| Area | Before | After |
|---|---|---|
| `/` route | Rendered a "Welcome back" launcher (`SystemDashboard`) | Redirects into a module (`RootRedirect`); `SystemDashboard` deleted |
| Dashboard | Products / Recipes / Calendar quick-stat cards linking out | Today hero + Next up + Water + This week cards; no quick-stat cards |
| Nav | Flat icon rail | 64px `ModuleRail` + 216px grouped `SectionPanel`, `ModuleSwitcher`, ⌘K `CommandPalette` (replaced the dead header search) |
| Nutrition summary | Custom from/to `DatePicker` + Apply; "Totals" / "Daily average" / "Goal progress" panels | `7 / 30 / 90 days` `SegmentedControl`; `MetricTile`s + bar chart + macro split |
| Import wizard | 3 steps (Continue → Validate → Confirm Import) | 2 steps (Continue auto-validates → `Import N days`) |
| Calendar week view | Day "cards", meals as links, hover-reveal edit/delete icons | `WeekGrid` ARIA grid; meals are chip `<button>`s with a "…" dropdown |
| Products / Recipes lists | `<table>` rows / cards with inline action buttons | CSS-grid `role="table"`/`role="list"`; actions behind a "…" dropdown menu |
| Hydration | Linear `role="progressbar"` | Bottle-fill `role="meter"`; shared glass-row add/remove |
| Notification channels | 3 rows (console / email / websocket) | 2 rows (email disabled, websocket toggle) — **console channel dropped** |

### Regressions / reductions worth a product decision

- **Weight-prediction calculator removed.** The interactive "type a hypothetical
  calorie target, watch BMR/TDEE recompute" card (`WeightPredictionCard`) is no
  longer rendered anywhere — the component file is now dead code. Its
  replacement (Profile → Overview "Energy model") is read-only and driven only
  by the saved goal, with no "enter a target" prompt and no distinct
  incomplete-profile message. `weight-prediction.spec.ts` shrank 9 → 6 tests.
- **"Console" notification channel** disappeared from the preferences UI
  (intentional simplification? the DTO field still exists).
- Several data tables were rebuilt as `<div>` CSS grids with no semantic
  markup; the audit added the appropriate ARIA roles (`table` / `row` /
  `columnheader` / `gridcell` / `listitem` + `aria-label`) back in
  `ProductList.tsx`, `RecipeCard.tsx` / `RecipeList.tsx`,
  `NutritionSummary.tsx`, `WeekGrid.tsx`, and `Hydration.tsx` — small a11y
  wins, not just test hooks.

### Flaky-test fixes (independent of the redesign)

- `diet-reminder-settings.spec.ts` → `mode: 'serial'` (tests raced over the
  shared per-user settings record) **and** its POM `save()` reordered to
  register `waitForResponse` before `click()` (fast-local-backend race).
- `weight-prediction.spec.ts` → `mode: 'serial'` + per-test profile seeding
  (was racing the shared profile/goals record under `fullyParallel`).
- `notifications/inbox.spec.ts` "mark read" → the list mock is now stateful, so
  the mutation's `onSettled` refetch doesn't revert the optimistic update.
- `recipes.page.ts` ingredient picker → click the listbox `option` explicitly
  (was relying on a blur-to-commit that races the product search) and centre
  the field first (the popover is `position: fixed`).

---

## Known coverage gaps (cross-cutting)

`diet-planner/logout.spec.ts` covers real menu logout: access/refresh revocation,
rejection of refresh-token reuse, cleared browser tokens, and Authentik's logout
confirmation. Run with `npm test -- diet-planner/logout.spec.ts` from `e2e/`.

Features that have no e2e coverage today:

- **Two-tier nav** — `ModuleRail` / `SectionPanel` grouping + collapse,
  `ModuleSwitcher`, the ⌘K `CommandPalette`.
- **`/` redirect logic** (`RootRedirect`) — 0 / 1 / many registered modules.
- **Mobile** — `BottomTabBar`, narrow-viewport layouts, sidebar drawer.
- **Dashboard cards' interactions** — Today hero goals CTA, Next up "Mark
  eaten", Water card glass-row, This week chart.
- **Calendar** — day view (`view: 'day'`), the meal-chip dropdown's other
  actions (Mark done / Record actual / Revert / bulk-complete), drag-to-move.
- **Weight-prediction** — the interactive calculator (removed; nothing to test).
- **User profile dropdown** — language switcher and settings deep-links.
- **Notification live-push** — real preferences and single/bulk inbox read now run against the backend in `notifications/real-inbox.spec.ts`; delivery to an already-open inbox remains uncovered. See [notifications](notifications.md).
- **Authentication failure paths** — invalid credentials, locked account, password reset.
- **i18n parity** — every text selector hard-codes English; no Polish coverage.
- **Accessibility** — no keyboard-only flows, no screen-reader assertions, no axe checks.
- **Visual regression** — `playwright/visual` baselines do not exist.

Tracking issues for each cluster of gaps would belong in the same milestone as the relevant feature work.
