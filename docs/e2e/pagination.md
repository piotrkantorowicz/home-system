# pagination.spec.ts — Pagination (Products & Recipes lists)

**Purpose**: list pagination contract on Products and Recipes — page-size selector exposes the canonical `[10, 25, 50, 100]` options, defaults to `25`, triggers a new API request when the user picks a different size, and disables the previous button on page 1.

**Setup**: each test navigates directly to `/diet-planner/products` or `/diet-planner/recipes`. No POM — assertions are inline because the same four tests run identically against both lists.

**Selectors**:

- Page-size select: `getByRole('combobox')`
- Previous button: `getByRole('button', { name: /previous/i })`
- API endpoints: `/api/v1/products` and `/api/v1/recipes` (GET)

## Tests

Each describe block runs the same four tests; details below apply to both lists.

### `page size selector is visible with the correct options`

- **Given** the user is on the list page
- **When** the page settles
- **Then** the combobox is visible within 8 s, and `select.locator('option').allTextContents()` exactly equals `['10', '25', '50', '100']`
- **Notes**: `toEqual` (deep equality) — adding a new size or reordering the existing ones will break this test, which is the intended contract.

### `page size selector defaults to 25`

- **Given** the user is on the list page
- **When** the page settles
- **Then** the combobox value is `25`
- **Notes**: defends the "25 per page" UX default — changing it requires intentionally updating the test.

### `changing page size triggers a new API request`

- **Given** the user is on the list page with combobox value `25`
- **When** `waitForResponse` is registered for `GET /api/v1/products` (or `/recipes`), then the combobox is set to `10`
- **Then** the response promise resolves and the combobox value becomes `10`
- **Notes**: register-before-act ordering matters — Playwright would otherwise miss a fast response that arrives before the listener is attached.

### `Previous button is disabled when on the first page`

- **Given** the user is on the list page (which always starts at page 1)
- **When** the page settles
- **Then** the Previous button is visible AND disabled
- **Notes**: pure UI assertion; doesn't exercise the click handler or URL state.

## Acceptance

List pagination on Products and Recipes presents the standard four page sizes, defaults to 25, fires a new request on size change, and disables Previous on page 1.

## Gaps

- Next button enabled-when-more-pages, disabled-when-last-page
- Clicking Next / Previous and asserting page-N data
- URL-state preservation across reloads (`?page=2&pageSize=10`)
- Calendar / nutrition pagination (Nutrition has its own pagination tested in [nutrition](nutrition.md))
- Empty-results state at page > 1 after a delete
