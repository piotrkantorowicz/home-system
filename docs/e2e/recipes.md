# recipes.spec.ts — Recipes CRUD

`test.describe.configure({ mode: 'serial' })` — edit and delete tests reuse the recipe created by the first test.

**Purpose**: recipe CRUD with at least one ingredient, plus reading nutrition-per-serving on the detail view.

**Setup**: the first test creates an ingredient product and a recipe referencing it. Subsequent tests look up that same recipe by its `Date.now()`-suffixed name. The auth fixture tracks both the product and recipe IDs for teardown.

**POM**: `pages/recipes.page.ts`:

- `createButton`: `getByRole('link', { name: /create recipe/i })`
- `searchFor(query)`: registers `waitForResponse` for `GET /api/v1/recipes` BEFORE filling the search input (so a fast response isn't missed); falls back to `networkidle` if the search term equals the current value (no API call)
- `recipeCardFor(name)`: a div containing both an `h*` heading with the name AND a `View` link — disambiguates from cards in other states
- `editRecipe(name)`: searches, finds the card, clicks the Edit link, waits for `/edit$` URL

## Tests

### `user can create a recipe with an ingredient`

- **Given** an ingredient product `Ingredient <ts>` is created via `ProductsPage.createProduct`
- **When** `createRecipe({ name: 'Recipe <ts>', servings: 2, prepTime: 15, ingredients: [{ name: 'Ingredient <ts>', amount: 100, unit: 'g' }] })` fills the form and saves
- **Then** the recipe appears in the list after searching for its name
- **Selectors of note**:
  - Ingredient product input: `getByPlaceholder(/search product/i)` — datalist-backed
  - Amount input: `getByPlaceholder('100')` — exact numeric placeholder
  - Unit select: `page.locator('select[name^="ingredients"]')` — targeted by name prefix because the product input also has `combobox` role
- **API**: `POST /api/v1/products` then `POST /api/v1/recipes`

### `user can view recipe details including the ingredient`

- **Given** the recipe from the previous test exists
- **When** the user searches for the recipe name, finds the card, clicks the View link
- **Then** the detail page shows `/nutrition per serving/i` and the ingredient name
- **Notes**: validates that the recipe-to-ingredient relationship persists and that nutrition is computed at the per-serving level (proving the backend's aggregation logic).

### `user can edit a recipe to update the number of servings`

- **Given** the recipe exists
- **When** the user opens the edit page, fills `getByLabel(/servings/i)` with `4`, and clicks `getByRole('button', { name: /save|update/i })`
- **Then** the URL becomes `/diet-planner/recipes/<id>` (detail) and the page shows `/4 serving/i`
- **API**: `PUT /api/v1/recipes/{id}`

### `user can delete a recipe and it disappears from the list`

- **Given** the recipe exists
- **When** `deleteRecipe(name)` opens the confirm dialog and clicks the Delete button (matched by `/^delete$/i` to avoid the heading)
- **Then** `expectRecipeNotVisible(name)` — no heading with that name is visible within 5 s
- **API**: `DELETE /api/v1/recipes/{id}`

## Acceptance

Recipes CRUD works end-to-end with computed nutrition per serving and ingredient persistence.

## Gaps

- Multi-ingredient recipes (only single-ingredient cases tested)
- Ingredient amount / unit edge cases (zero, very large, mixed units)
- Instructions field (`getByLabel(/instructions/i)` is supported by the POM but no test uses it)
- Prep-time validation (out-of-range values)
- Dietary tags / categories
- Recipe scaling math when servings change
- Adding/removing ingredients on edit
- Search by ingredient name (not just recipe name)
