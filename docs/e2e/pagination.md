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

### `Next/Previous navigate a search-scoped result set with correct bounds` (Products only)

- **Given** 12 products are seeded via API, all sharing one unique timestamped
  name prefix (`seedProducts`, `utils/seed.ts`), then the list is searched for
  that prefix and page size set to 10
- **When** page 1 loads, Next is clicked, then Previous is clicked
- **Then** page 1 shows rows 1 and 10 (not 11), Previous disabled / Next
  enabled; page 2 shows rows 11 and 12 (not 1), Next disabled / Previous
  enabled; back on page 1 the same bounds hold again
- **Notes**: the search prefix is what makes the bounds exact rather than
  "at least N" — products search is household-wide, so an un-scoped list's
  total count depends on everything every other spec has ever created.

### `search and page size survive a reload via the URL, and clearing search widens the results` (Products only)

- **Given** 3 products are seeded with a unique prefix, then searched for and
  page size set to 10
- **When** the URL is checked, the page is reloaded, then the search is
  cleared
- **Then** the URL carries `search=<prefix>` and `pageSize=10`; after reload
  the search input, page-size select, and filtered rows are unchanged (proof
  the state came from the URL, not React state that a reload would drop);
  after clearing, `search` leaves the URL and the "N items" count grows past
  the seeded 3 (proof the unscoped list, not an empty one, came back)

## Acceptance

List pagination on Products and Recipes presents the standard four page sizes, defaults to 25, fires a new request on size change, disables Previous/Next at the correct bounds, and persists search + page size through a reload via the URL.

## Gaps

- Jump-to-last-page or page-number input
- Page indicator text ("Page 1 of N")
- Calendar / nutrition pagination (Nutrition has its own pagination tested in [nutrition](nutrition.md))
- Empty-results state at page > 1 after a delete
- Next/Previous bounds and URL-state persistence are only exercised on
  Products — Recipes shares the same `Pagination` + `useListLocation`
  components, so the risk of a Recipes-only regression is low
