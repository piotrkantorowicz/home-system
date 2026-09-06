# dashboard.spec.ts — Dashboard

**Purpose**: smoke that the redesigned diet-planner dashboard renders and its quick actions and section cards work.

> **#208 redesign** — the dashboard no longer shows Products / Recipes / Calendar
> quick-stat cards (that entry point moved into the two-tier nav's grouped
> section panel, and `/` now redirects into a module rather than rendering a
> launcher). It now shows a **Today** hero (calories left / eaten / target +
> macro bars), a **Next up** meal card, a **Water** card, and a **This week**
> review card. `SystemDashboard` was deleted.

**Setup**: `DashboardPage.goto()` navigates to `/diet-planner` and waits for
`networkidle`. No data setup.

**POM**: `pages/dashboard.page.ts`:

- `heading` — `getByRole('heading', { name: 'Today', level: 1 })`
- `logWaterLink` — `getByRole('link', { name: /log water/i })`
- `logMealButton` — `getByRole('button', { name: /log a meal/i }).first()` (the
  same label also appears on the Next up card's empty-state CTA; both open the
  MealForm sheet)
- `fullPlanLink` — `getByRole('link', { name: /full plan/i })` (on the Next up card)

## Tests

### `shows the today hero and quick actions on load`

Heading "Today", "Log water" link, and "Log a meal" button are all visible.

### `water card and week review card are visible`

The `Water` and `This week` section headings render.

### `the "Full plan" link on the Next up card opens the calendar`

Clicking `Full plan` navigates to `/diet-planner/calendar`.

### `"Log water" opens the hydration page`

Clicking `Log water` navigates to `/diet-planner/hydration`.

### `"Log a meal" opens the meal form sheet`

Clicking `Log a meal` opens a `role="dialog"` (the MealForm).

## Acceptance

The dashboard is reachable, the four cards render, and the quick-action
navigation contracts hold.

## Gaps

- Today hero empty-state (no goals configured) — the "Set goals" CTA
- Macro-bar values / progress against goal
- Next up card meal completion ("Mark eaten") + optimistic toast
- Water card glass-row add/remove (shared with [hydration](hydration.md))
- This week review bar chart values
- Mobile layout
