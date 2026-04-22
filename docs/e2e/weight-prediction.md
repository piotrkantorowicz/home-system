# weight-prediction.spec.ts — Weight prediction

**Purpose**: WeightPredictionCard on the dashboard — calorie input, prediction stats (BMR, TDEE, weekly change, BMI), and incomplete-profile fallback.

**Setup**: most tests set up a profile via `ProfilePage` first, then navigate to the dashboard. The card prefills its calorie input from the user's goals, so `enterCalories` short-circuits when the requested value already equals the prefilled value (React Query returns cached data, no new HTTP call).

## Tests

- `prediction card is visible on the dashboard`
- `shows prompt to enter calories when input is empty` — clears the input, asserts the prompt
- `displays BMR, TDEE and weekly change after entering calorie target` — full prediction visible
- `BMR and TDEE are positive numeric values`
- `shows weight loss trend when calorie target is below TDEE` — weekly change shows minus prefix
- `shows weight gain trend when calorie target is above TDEE` — plus prefix
- `shows estimated goal date when current and target weights differ`
- `prediction updates when calorie target is changed` — different inputs produce different weekly changes
- `returns 404 and shows incomplete-profile message when profile lacks required fields` — uses `page.route` to mock 404

## Acceptance

The weight-prediction card calculates and displays correctly for a complete profile, and degrades gracefully when the backend can't compute a prediction.

## Gaps

- BMI category boundary checks (underweight / overweight / obese)
- Goal-already-reached state
- Calorie input below `min=500` or above `max=10000`
- Prediction across multiple goal types
