# import.spec.ts — Diet Plan Import

`test.describe.configure({ mode: 'serial', timeout: 120000 })`.

**Purpose**: the import wizard — file and paste input, validation and recovery,
full import, and bulk weekly-plan visibility on the calendar.

> **#208 redesign — wizard collapsed from 3 steps to 2.** "Continue" now
> auto-validates (no separate "Validate" button, no distinct "step 3" screen).
> Steps are `upload` → `review` → `done`. The review step shows the validation
> result inline (a ready / warning / error `Banner`) and an
> `Import <N> days` button that does the actual import and jumps straight to
> `done` → `/diet-planner/calendar`.

**Setup**: each test generates its own `Date.now()`-suffixed JSON payload.
Created entities are tracked by the auth fixture and cleaned up by global
teardown.

**POM**: `pages/import.page.ts`:

- `jsonInput` — `getByLabel(/diet plan json/i)` (the paste textarea)
- `continueButton` — `getByRole('button', { name: /^continue$/i })`
- `importButton` — `getByRole('button', { name: /^import \d+ days?$/i })`
- `loadSampleButton` — `getByRole('button', { name: /load sample/i })`
- `reviewDetected` — `getByText('Detected', { exact: true })` (the review-step landmark)
- `uploadButton` / `uploadJson(json)` — use the native file chooser with an in-memory `.json` file
- `previousButton` — return from Review to Upload without importing
- `runImportWizard(json?)`:
  - set / load JSON
  - register `waitForResponse('POST /meals/validate')`, click Continue, await it,
    await `reviewDetected` visible
  - register `waitForResponse('POST /meals/import')`, click `Import N days`,
    await it (throws on non-2xx), await URL `/diet-planner/calendar`

## Tests

### `user can load sample JSON and it populates the input`

`loadSample()` → `jsonInput.inputValue()` contains `products` and `recipes`.

### `entering invalid JSON blocks progression to step 2`

Paste `{ this is not valid json }`, click Continue → `/invalid json/i` error is
visible (client-side parse; no API call).

### `uploaded JSON can be reviewed, backed out, and imported`

Upload a `.json` file through the file chooser, Continue to Review, and click
Previous. The JSON remains in the upload field; a real products API query
confirms nothing was created. Continue again, import, then verify the recipe's
meal chip appears on the calendar.

### `row validation errors block import`

Submit a product with no name, a recipe ingredient with no product, and an
invalid schedule date. Review shows each actionable server validation message
and disables Import.

### `duplicate product warning allows review to continue`

Submit two same-named products. The server reports one nonblocking warning;
Review shows its message and leaves Import enabled. This payload is not
imported because it only exercises the warning state.

### `continuing past step 1 auto-validates and shows a ready-to-import summary`

Paste a well-formed payload, click Continue → after the `POST /meals/validate`
response, `Detected` + `Ready to import` are visible and the `Import 1 days`
button is enabled. (Replaces the old separate-validate-then-step-3 test.)

### `full import wizard creates products, recipes and calendar entries`

`runImportWizard(data)` → URL becomes `/diet-planner/calendar`.
**API**: `POST /meals/validate` then `POST /meals/import`.

### `imported weekly plan meals are visible on the calendar`

A 4-meal-types × 7-days payload → after import:

- each of the 4 recipes appears as a meal **chip `<button>`** exactly 7 times
  (chip accessible name is `<recipe> <kcal> kcal`, so a substring name match works)
- all 7 weekday **column headers** (`role="columnheader"`) are visible

## Acceptance

The wizard accepts a `.json` file or pasted JSON, shows validation errors and
warnings, preserves the file contents on Back, and imports valid data into the
calendar. Bulk weekly plans render correctly.

## Gaps

- Re-check after editing a previously invalid payload
- Large payloads, conflicting names, idempotency
