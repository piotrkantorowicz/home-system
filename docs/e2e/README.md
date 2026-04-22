# E2E Tests

The Playwright end-to-end suite at `src/E2ETests/` covers **82 tests across 13 specs**, plus one auth setup (83 entries in total). It runs serially against a real backend, real frontend, and real Authentik (no mocks except for one explicit `route.fulfill` 404 case in `weight-prediction.md`).

This directory is the human-readable reference. Each spec has its own page below; this README is the hub for cross-cutting concerns and discovery.

---

## Specs

| Spec | What it covers | Tests |
|---|---|---|
| [dashboard](dashboard.md) | Stat cards on the diet-planner dashboard | 3 |
| [import](import.md) | Multi-step import wizard end-to-end | 5 |
| [meals](meals.md) | Calendar CRUD and week navigation | 6 |
| [pagination](pagination.md) | List pagination on Products and Recipes | 8 |
| [products](products.md) | Products CRUD and search | 5 |
| [recipes](recipes.md) | Recipes CRUD with ingredients | 4 |
| [nutrition](nutrition.md) | Nutrition summary, totals, goal progress | 11 |
| [profile](profile.md) | Biometrics form on the profile hub | 6 |
| [meal-schedule](meal-schedule.md) | Meal slots on the profile hub | 9 |
| [hydration](hydration.md) | Hydration page + settings | 6 |
| [notification-preferences](notification-preferences.md) | Notification prefs on the profile hub | 8 |
| [weight-prediction](weight-prediction.md) | WeightPredictionCard on the dashboard | 9 |
| [theme](theme.md) | Theme toggle in the user-menu dropdown | 2 |

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

---

## Auth fixture behaviour

Every test starts by trying to refresh the stored OIDC tokens via the refresh-token grant. On success it injects fresh tokens into `localStorage` before navigation (~200 ms). On failure (token revoked / Authentik restart), it falls back to a full interactive login through the Authentik UI and rewrites `playwright/.auth/user.json` for subsequent tests.

The fixture also subscribes to all responses and tracks IDs of POSTed products and recipes (URLs matching `/api/v1/products` and `/api/v1/recipes`) into `playwright/.test-data.json`. The global teardown reads this file and DELETEs each tracked entity through the API. Meals are not tracked individually — teardown deletes all meals for the test user unconditionally.

---

## Conventions

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
| [import](import.md) | Each test creates products/recipes with `Date.now()`-suffixed names; serial avoids race conditions on the import wizard. |
| [meals](meals.md) | All tests reuse a single weekly plan imported by the first `setup:` test. Later tests edit/delete meals from that plan. |
| [nutrition](nutrition.md) (with-meal-data describe) | Same pattern — first test imports the plan, later tests assert nutrition aggregates. |
| [recipes](recipes.md) | Edit and delete tests reuse the recipe created by the first test. |

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
