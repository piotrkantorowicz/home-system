# dashboard.spec.ts — Dashboard

**Purpose**: smoke that the diet-planner dashboard renders its three module cards (Products, Recipes, Calendar), shows numeric counts, and links into the right pages.

**Setup**: `DashboardPage.goto()` navigates to `/diet-planner` and waits for `networkidle`. No data setup — uses whatever counts the backend reports for the test user.

**POM**: `pages/dashboard.page.ts` exposes locators by `data-testid`:

- `product-card`, `recipe-card`, `calendar-card` — the three stat cards
- `product-count`, `recipe-count`, `calendar-count` — the numeric children inside each card

## Tests

### `all three stat cards are visible on load`

- **Given** the user is on `/diet-planner`
- **When** the page finishes loading
- **Then** all three `data-testid` cards (`product-card`, `recipe-card`, `calendar-card`) are visible
- **Notes**: pure smoke — proves the dashboard renders without crashing and the three module entry points exist.

### `stat cards show numeric counts after data loads`

- **Given** the user is on `/diet-planner`
- **When** `expectStatsLoaded()` waits up to 10 s for each count to be visible
- **Then** each count locator's text matches `/\d+/` (one or more digits)
- **Notes**: doesn't assert specific counts (depends on user data); only proves that numeric content is rendered, not "loading" or "—" placeholders.

### `each stat card links to its respective page`

- **Given** the user is on `/diet-planner`
- **When** the user clicks each card in turn (with `page.goBack()` between)
- **Then** clicking `product-card` navigates to `/diet-planner/products`; `recipe-card` → `/diet-planner/recipes`; `calendar-card` → `/diet-planner/calendar`
- **Notes**: `toHaveURL` is exact (no trailing wildcards) so the test catches accidental query-param leaks or unintended sub-routes.

## Acceptance

The dashboard is reachable, the three module cards render with numeric counts, and their navigation contracts hold.

## Gaps

- Goals CTA card (when no goals configured)
- Inline goal progress card (when goals exist) — protein / carbs / fat / fiber bars
- WeightPredictionCard (covered separately by [weight-prediction](weight-prediction.md))
- Welcome / empty-state messaging
- Avatar / username / theme toggle in the header (covered partially by [theme](theme.md))
- Mobile sidebar drawer behaviour
