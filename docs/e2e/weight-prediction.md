# weight-prediction.spec.ts — Weight prediction

**Purpose**: WeightPredictionCard on the dashboard — calorie input, prediction stats (BMR, TDEE, weekly weight change, current/target BMI, estimated goal date), and the incomplete-profile fallback message.

**Setup**: most tests set up a complete profile via `ProfilePage` first (with date of birth, gender, height, weight, activity level), then navigate to the dashboard via `WeightPredictionPage.goto()` → `/diet-planner`.

The card prefills its calorie input from the user's goals (`goalCalories` prop on `WeightPredictionCard`). When the test's requested calorie value equals the prefilled value, React Query short-circuits with the cached prediction and no new HTTP request fires. To handle this, `enterCalories(calories)` checks the current input value and returns immediately if it already matches — relying on the assertions that follow to read the already-rendered prediction.

**POM**: `pages/weight-prediction.page.ts`:

- `calorieTargetInput`: `getByLabel(/daily calorie target/i)` (the `Daily Calorie Target (kcal)` label)
- Stat cards located via the visible label text and a parent navigation:
  - `bmrValue`: `getByText('BMR', { exact: true }).locator('xpath=..').locator('p.text-2xl')`
  - `tdeeValue`, `weeklyChangeValue`, `currentBmiValue`, `targetBmiValue`: same pattern
  - `goalDateValue`: uses `p.text-xl` (smaller font for the date string)
- State messages located by their i18n strings:
  - `noProfileMessage`: `getByText(/set up your biometrics profile/i)`
  - `enterCaloriesMessage`: `getByText(/enter a daily calorie target/i)`
  - `incompleteProfileMessage`: `getByText(/complete your profile/i)`
- `enterCalories(calories)`: short-circuits when current input value equals requested value; otherwise registers `waitForResponse('/api/v1/profile/prediction')`, fills the input, awaits the response.

## Tests

### `prediction card is visible on the dashboard`

- **Given** the user navigates to `/diet-planner` via `predictionPage.goto()`
- **When** the page settles
- **Then** `calorieTargetInput` is visible
- **Notes**: the simplest possible smoke that the card renders on the dashboard (post-#116 it moved off the profile page).

### `shows prompt to enter calories when input is empty`

- **Given** the user is on the dashboard
- **When** the user clears `calorieTargetInput` (in case it was prefilled from goals)
- **Then** `enterCaloriesMessage` (`/enter a daily calorie target/i`) is visible
- **Notes**: the clear step decouples the test from goals state — the prompt only renders when `hasProfile && !debouncedCalories`.

### `displays BMR, TDEE and weekly change after entering calorie target`

- **Given** a complete profile is saved (`gender: Male`, `heightCm: 180`, `currentWeightKg: 80`, `targetWeightKg: 75`, `activityLevel: ModeratelyActive`, dob `1990-05-15`)
- **When** the user navigates to `/diet-planner` and `enterCalories(2000)` runs
- **Then** `expectPredictionVisible()` — bmrValue, tdeeValue, weeklyChangeValue, currentBmiValue all visible (15 s timeout)
- **API**: `GET /api/v1/profile/prediction?dailyCalorieTarget=2000`

### `BMR and TDEE are positive numeric values`

- **Given** the same complete profile
- **When** `enterCalories(2000)` runs and `bmrText`, `tdeeText` are read via `textContent()`
- **Then** `Number(bmrText.trim()) > 0` and `Number(tdeeText.trim()) > 0`
- **Notes**: the trim is needed because the `<p>` tag may contain whitespace; the assertion guards against the regression where a 0 or NaN slips through.

### `shows weight loss trend when calorie target is below TDEE`

- **Given** the profile is set with `targetWeightKg: 75` (below current `80`) and `enterCalories(1500)` runs (well below the TDEE of ~2720 for this profile)
- **When** the prediction renders
- **Then** `weeklyChangeValue.textContent()` contains `−` (Unicode minus, U+2212)
- **Notes**: validates the surplus/deficit display logic — uses the typographic minus, not ASCII hyphen. Changing the i18n template would break this.

### `shows weight gain trend when calorie target is above TDEE`

- Symmetric: profile has `targetWeightKg: 85` (above current `80`), `enterCalories(3500)` triggers a surplus, and the assertion checks for `+` in the weekly-change text.

### `shows estimated goal date when current and target weights differ`

- **Given** profile with `currentWeightKg: 90`, `targetWeightKg: 80` (different) and `enterCalories(1800)` runs
- **When** the prediction completes
- **Then** `goalDateValue` is visible and its text content has length > 0
- **Notes**: doesn't parse the date; only proves the field is populated. Format depends on locale.

### `prediction updates when calorie target is changed`

- **Given** profile setup, navigate to dashboard
- **When** `enterCalories(1500)` runs and `weeklyTextLow` is captured, then `enterCalories(2500)` runs and `weeklyTextHigh` is captured
- **Then** `weeklyTextLow !== weeklyTextHigh`
- **Notes**: proves the prediction is reactive to input changes (no stale display). Both calls hit the API with different `dailyCalorieTarget` query strings, so React Query invalidates between them.

### `returns 404 and shows incomplete-profile message when profile lacks required fields`

- **Given** the test mocks `GET /api/v1/profile/prediction*` with `route.fulfill({ status: 404, body: '' })`
- **When** the user navigates to the dashboard and `enterCalories(2000)` runs
- **Then** `incompleteProfileMessage` (`/complete your profile/i`) is visible
- **Notes**: this is the only test in the suite that uses `page.route` to mock a backend response — all others go through the real backend.

## Acceptance

The weight-prediction card calculates and displays correctly for a complete profile, reacts to calorie input changes, and degrades gracefully when the backend can't compute a prediction (404 → incomplete-profile message).

## Gaps

- BMI category boundary checks (underweight < 18.5, overweight ≥ 25, obese ≥ 30 — none of the test profiles cross a boundary)
- Goal-already-reached state (currentWeight == targetWeight)
- Calorie input below `min=500` or above `max=10000` (HTML5 validation only)
- Prediction across multiple goal types (lose / maintain / gain — the `targetWeightKg` field encodes intent indirectly)
- Loading state UI between prediction request and response
- Real 500-error response (only 404 is mocked)
