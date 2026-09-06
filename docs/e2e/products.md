# products.spec.ts — Products

**Purpose**: products CRUD and search — happy paths for create, read (search), update, delete plus client-side validation for required name.

**Setup**: each test creates a uniquely-named product (`Date.now()` suffix) so parallel runs and cleanup don't collide. Created IDs are tracked by the auth fixture (responses to `POST /api/v1/products`) and deleted in the global teardown.

> **#208 redesign** — the list is a CSS-grid, not a `<table>`. The table view's
> container carries `role="table"`, header/rows `role="row"`, each data row
> `role="row"` + `aria-label={name}`; the card view is `role="list"` with
> `role="listitem"` + `aria-label={name}` cards (both added in `ProductList.tsx`
> so role-based locators still work). Row actions (Edit / Delete) moved behind a
> "…" dropdown menu whose content Radix portals to `document.body`.

**POM**: `pages/products.page.ts`:

- `createButton`: `getByRole('link', { name: /add product/i })` — link to `/products/new`
- `searchInput`: `getByPlaceholder(/search/i)`
- `createProduct(data)`: navigates to the new-product form, fills name + nutrition spinbuttons, clicks Save, waits for redirect to the list page
- `searchFor(query)`: fills the search input and waits for `GET /api/v1/products`
- `rowFor(name)`: `getByRole('row', { name })`
- `openRowMenu(row)`: clicks the row's `Actions` ("…") button; Edit / Delete are
  then queried at the page level as `getByRole('menuitem', { name: /^edit$/i | /^delete$/i })`

## Tests

### `user can create a product with nutrition values`

- **Given** the user is on `/diet-planner/products` (clean state for the new product name)
- **When** `createProduct({ name: 'E2E Product <ts>', calories: 150, protein: 12, carbs: 18, fat: 6 })` runs
- **Then** the URL becomes `/diet-planner/products` (redirect after save) and the new product row is visible after searching for the name
- **Selectors**: form fields use `getByRole('spinbutton', { name: /calories|protein|carbs|^fat/i })` — note the anchored `^fat` to disambiguate from the (absent) "saturated fat" field
- **API**: `POST /api/v1/products` — response is intercepted by the auth fixture and the new ID is added to `playwright/.test-data.json` for teardown.

### `user can search for products by name`

- **Given** the user is on `/diet-planner/products`
- **When** the user searches for `chicken`
- **Then** either the table or the empty-state message (`/no products/i`) is visible within 10 s
- **Notes**: tolerates an empty backend; the test only proves the search input dispatches a request and the UI handles both result shapes.

### `product form shows required-field error when name is empty`

- **Given** the user is on the new-product form
- **When** the user clicks Save without filling the name
- **Then** the error message `/product name is required/i` is visible
- **Notes**: tests the client-side zod validation on the name field — no API call is made.

### `user can edit an existing product`

- **Given** a product `Edit Product <ts>` exists with `calories: 100`
- **When** `editProduct(name, { calories: 250 })` navigates to the edit page, updates calories, and clicks Save
- **Then** the detail page shows the text `/250/`
- **Selectors**: `openRowMenu(row)` opens the "…" menu, then `getByRole('menuitem', { name: /^edit$/i })`; Save button matches `/save|update/i` to handle the difference between create and edit copy
- **API**: `PUT /api/v1/products/{id}` — followed by URL change to the product detail route

### `user can delete a product and it disappears from the list`

- **Given** a product `Delete Product <ts>` exists
- **When** `deleteProduct(name)` opens the confirm dialog and clicks Delete
- **Then** after re-navigating to the list and searching, `expectProductNotVisible(name)` passes
- **Selectors**: `openRowMenu(row)` → `getByRole('menuitem', { name: /^delete$/i })` → confirm dialog is `getByRole('dialog')` containing `/delete product/i`; the inner Delete button is matched by `/^delete$/i`
- **API**: `DELETE /api/v1/products/{id}`

## Acceptance

Products CRUD surface works with form validation. Created entities are cleaned up automatically by the global teardown.

## Gaps

- Macro percentage validation (e.g., protein + carbs + fat shouldn't exceed 100% of calories)
- Server-side error display (e.g., 409 conflict on duplicate name)
- Unit selector (g / ml / oz) — defaulted to `g` in the POM
- Fiber field (only filled when explicitly passed)
- Optional fields (description, brand, source URL) not exercised
- Bulk operations (delete many, import from CSV)
- Search debounce and clearing
- URL-state preservation for active search query
