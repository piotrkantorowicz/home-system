# import.spec.ts — Diet Plan Import

`test.describe.configure({ mode: 'serial', timeout: 120000 })` — each test creates new entities; serial avoids parallel-write races on the import API.

**Purpose**: end-to-end coverage of the multi-step import wizard — JSON validation, server-side validation, full import, and bulk weekly-plan visibility on the calendar.

**Setup**: each test generates its own JSON payload with `Date.now()`-suffixed names so they don't collide. Created products and recipes are tracked by the auth fixture and cleaned up in the global teardown.

**POM**: `pages/import.page.ts`:

- `jsonInput`: `getByLabel(/json/i)` — the multi-line textarea
- `continueButton`: `getByRole('button', { name: /continue/i })` — step 1 → 2
- `validateButton`: `getByRole('button', { name: /validate data|re-validate/i })` — step 2
- `importButton`: `getByRole('button', { name: /confirm.*import/i })` — step 3
- `loadSampleButton`: `getByRole('button', { name: /load sample/i })`
- `runImportWizard(json?)`: drives the full 3-step flow:
  - Step 1 → 2: paste/load JSON, click Continue, await `/step 2/i` text
  - Step 2 → 3: click Validate, await `POST /meals/validate` response, await `/step 3/i` text
  - Step 3: click Confirm Import, await `POST /meals/import` response, await URL `/diet-planner/calendar`
- `clickAndWaitForResponse(button, urlPattern, successLocator?)`: registers `waitForResponse` (`POST` matching pattern, 30 s) BEFORE clicking; throws on non-2xx response

## Tests

### `user can load sample JSON and it populates the input`

- **Given** the user is on `/diet-planner/import`
- **When** `loadSample()` clicks the Load Sample button
- **Then** `jsonInput.inputValue()` contains both `'products'` and `'recipes'` (substring assertions on the JSON payload)
- **Notes**: doesn't actually run the import — proves the sample-JSON affordance populates the field.

### `entering invalid JSON blocks progression to step 2`

- **Given** the user is on the import page
- **When** the user pastes `'{ this is not valid json }'` and clicks Continue
- **Then** an error message `/invalid json/i` is visible
- **Notes**: client-side JSON parse validation — no API call.

### `validation step shows success before reaching step 3`

- **Given** the user is on the import page with a well-formed JSON payload (one product, one recipe referencing the product, one schedule entry for today)
- **When** the user pastes the JSON, clicks Continue, awaits `/step 2/i`, then clicks Validate
- **Then** `/step 3/i` is visible within 15 s AND `/validation successful/i` is visible
- **API**: `POST /meals/validate` (server validates the parsed payload — checks recipe references, product references, etc.)
- **Notes**: tests the server-side validation step without committing the import — useful for catching schema drift before the destructive write.

### `full import wizard creates products, recipes and calendar entries`

- **Given** the user is on the import page with a JSON payload (one product `Import Product <ts>`, one recipe `Import Recipe <ts>` referencing it, one schedule entry for today)
- **When** `runImportWizard(data)` drives all three steps
- **Then** the URL becomes `/diet-planner/calendar`
- **API**: `POST /meals/validate` then `POST /meals/import`
- **Notes**: the full happy path. Created entities are tracked for teardown via the response interceptor in the auth fixture.

### `imported weekly plan meals are visible on the calendar`

- **Given** the user is on the import page with a 4-meal-types × 7-days payload (4 products, 4 recipes, 28 schedule entries — Monday to Sunday of the current week)
- **When** `runImportWizard(payload)` runs and then the calendar renders
- **Then**:
  - Each of the 4 recipes appears as a calendar link exactly **7 times** (once per day)
  - All 7 weekday headers (`Mon`, `Tue`, …, `Sun`) are visible
  - The text `/no meal/i` appears exactly **0 times** (every slot is filled for this plan)
- **Notes**: the most ambitious test in the suite — proves the entire create-many-products + create-many-recipes + bulk-schedule flow plus that the calendar renders all 28 slots correctly. Days computed via `startOfWeek(new Date(), { weekStartsOn: 1 })` and `addDays`, formatted with `format(d, 'yyyy-MM-dd')`.

## Acceptance

The wizard round-trips a JSON payload into actual products, recipes, and calendar entries. Bulk imports of full weekly plans render correctly on the calendar.

## Gaps

- Error handling when validation reports specific row-level failures (e.g., "row 3: recipe X not found")
- Undo / cancel flow (closing the wizard mid-step)
- Large-payload behaviour (e.g., 100+ products or recipes — does the wizard timeout?)
- Partial-failure recovery (some products created, then a later step fails)
- Import with conflicting names (existing products with the same name)
- Import idempotency (running the same payload twice — does it duplicate or skip?)
- Schema validation for malformed objects (e.g., missing required fields)
- File-upload variant (the wizard accepts pasted JSON; if a file-upload UI is added, that path is uncovered)
