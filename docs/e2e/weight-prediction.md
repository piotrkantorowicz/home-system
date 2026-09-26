# weight-prediction.spec.ts — Weight prediction (Profile overview → Energy model)

**Purpose**: the read-only "Energy model" card on the Profile overview page —
BMR, TDEE, daily deficit, weekly weight change, current / target BMI, driven by
the user's saved `dailyCalorieTarget` goal.

> **#208 redesign — significant behaviour change.** The interactive
> `WeightPredictionCard` (its own `Daily Calorie Target` input, a live
> "type a hypothetical number and watch BMR/TDEE recompute" calculator, an
> "enter a calorie target" prompt, and a distinct "complete your profile"
> 404 message) is **gone** — the component file still exists but nothing
> imports it. What replaced it is a read-only `MetricTile` grid on
> `ProfileOverview` that only renders when `goals.dailyCalorieTarget` is set
> (via **Profile → Goals**). There is no in-place override and no explicit
> incomplete-profile messaging any more — an unset goal or a failed
> prediction both just show "Set a daily calorie target to see your energy
> model." This is a real reduction in capability; see the suite README's
> findings section.

`test.describe.configure({ mode: 'serial' })` — every test reads/writes the same
per-user profile + goals record.

**Setup**: `beforeEach` seeds a complete biometrics profile via `ProfilePage`
(`dob 1990-05-15`, `Male`, `180 cm`, `80 kg` current, `75 kg` target,
`ModeratelyActive`). Tests then vary only the calorie goal.

**POM**: `pages/weight-prediction.page.ts`:

- `goto()` → `/diet-planner/profile` (Overview section)
- `setDailyCalorieTarget(n)` → navigates to `?section=goals`, fills
  `#dailyCalorieTarget`, clicks `Save goals`, awaits the `POST`/`PUT
  /api/v1/goals` response
- `bmrValue` / `tdeeValue` / `weeklyChangeValue` / `currentBmiValue` /
  `targetBmiValue` — `getByText(<label>, { exact: true }).locator('xpath=..').locator('.numeral')`
  (MetricTile renders label + value as sibling divs; the value div has class `numeral`)
- `energyModelEmptyMessage` — `getByText(/set a daily calorie target/i)`

## Tests

### `shows the empty-state hint when no calorie goal is set`

Mocks `GET /api/v1/goals` → `null` (a goal is usually already set for the
worker user via cross-spec state), then asserts the empty-state hint.

### `displays BMR, TDEE and weekly change once a calorie goal is set`

`setDailyCalorieTarget(2000)` → the BMR / TDEE / Weekly change / Current BMI
tiles are all visible.

### `BMR and TDEE are positive numeric values`

Reads the tile text, strips the U+2009 thin-space thousands separator
(`replace(/[^\d]/g, '')`), asserts `> 0`.

### `shows a weight-loss trend when the calorie goal is below TDEE`

`setDailyCalorieTarget(1500)` → the Weekly change tile text contains `−`
(U+2212).

### `shows a weight-gain trend when the calorie goal is above TDEE`

`setDailyCalorieTarget(3500)` → the Weekly change tile text contains `+`.

### `the prediction changes when the calorie goal changes`

`setDailyCalorieTarget(1500)` then `2500` → the two Weekly change readings differ.

## Acceptance

The Energy model card computes and displays for a complete profile and reacts
to changes in the saved calorie goal.

## Gaps

- BMI category boundaries / colours
- Estimated goal date (still rendered on the Identity card, not asserted)
- Real 500 error from the prediction endpoint
- Loading state
