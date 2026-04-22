# pagination.spec.ts — Pagination (Products & Recipes lists)

**Purpose**: list pagination contract — page size selector and previous-button disabled state. The same four assertions run against both the Products list and the Recipes list.

**Setup**: none.

## Tests

Per list (both lists tested identically):

- `page size selector is visible with the correct options` — `[10, 25, 50, 100]`
- `page size selector defaults to 25`
- `changing page size triggers a new API request` — verified via `waitForResponse`
- `Previous button is disabled when on the first page`

## Acceptance

List pagination on Products and Recipes presents the standard four page sizes and disables the previous button at page 1.

## Gaps

- Next button enabled-when-more-pages
- Jump-to-last-page
- Page indicator text
- URL-state preservation across reloads
- Calendar / nutrition pagination
