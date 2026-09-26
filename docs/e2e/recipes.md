# recipes.spec.ts — Recipes CRUD

`test.describe.configure({ mode: 'serial' })` — edit and delete tests reuse the recipe created by the first test.

**Purpose**: recipe CRUD with at least one ingredient, plus reading nutrition-per-serving on the detail view.

**Setup**: the first test creates an ingredient product and a recipe referencing it. Subsequent tests look up that same recipe by its `Date.now()`-suffixed name. The auth fixture tracks both the product and recipe IDs for teardown.

> **#208 redesign** — the recipe grid is `role="list"` and each `RecipeCard`
> root carries `role="listitem"` + `aria-label={recipe.name}` (there is no
> heading in the card — the name is a plain link). View / Edit / Delete moved
> behind a "…" dropdown menu, portaled to `document.body`. The recipe-form
> ingredient field is a type-ahead **combobox** (`ProductPicker`) with a
> `role="listbox"` of `role="option"` results — the POM clicks the matching
> option explicitly rather than relying on blur-to-commit (a race against the
> in-flight product search).

**POM**: `pages/recipes.page.ts`:

- `createButton`: `getByRole('link', { name: /create recipe/i })`
- `searchFor(query)`: no-op when the search input already holds `query` (no request fires); otherwise registers `waitForResponse` for `GET /api/v1/recipes` BEFORE filling the search input
- `recipeCardFor(name)`: `getByRole('listitem', { name })`
- `openCardMenu(card)`: clicks the card's `Actions` button; `viewRecipe` /
  `editRecipe` / `deleteRecipe` then click the portaled
  `getByRole('menuitem', { name: /^view$|^edit$|^delete$/i })`

## Tests

### `user can create a recipe with an ingredient`

- **Given** an ingredient product `Ingredient <ts>` is created via `ProductsPage.createProduct`
- **When** `createRecipe({ name: 'Recipe <ts>', servings: 2, prepTime: 15, ingredients: [{ name: 'Ingredient <ts>', amount: 100, unit: 'g' }] })` fills the form and saves
- **Then** the recipe appears in the list after searching for its name
- **Selectors of note**:
  - Ingredient product input: `getByPlaceholder(/search product/i)` — a
    `role="combobox"`; the POM centres it in the viewport (the listbox is a
    `position: fixed` popover), types the name, then clicks the
    `getByRole('option', { name })` with `{ force: true }`
  - Amount input: `getByPlaceholder('100')` — exact numeric placeholder
  - Unit select: `getByRole('combobox', { name: /^unit/i })`
- **API**: `POST /api/v1/products` then `POST /api/v1/recipes`

### `user can view recipe details including the ingredient`

- **Given** the recipe from the previous test exists
- **When** `viewRecipe(name)` opens the card's "…" menu and clicks the portaled `View` menuitem
- **Then** the detail page shows `/nutrition per serving/i` and the ingredient name
- **Notes**: validates that the recipe-to-ingredient relationship persists and that nutrition is computed at the per-serving level (proving the backend's aggregation logic).

### `user can edit a recipe to update the number of servings`

- **Given** the recipe exists
- **When** the user opens the edit page, fills `getByLabel(/servings/i)` with `4`, and clicks `getByRole('button', { name: /save|update/i })`
- **Then** the URL becomes `/diet-planner/recipes/<id>` (detail) and the page shows `/4 serving/i`
- **API**: `PUT /api/v1/recipes/{id}`

### `user can delete a recipe and it disappears from the list`

- **Given** the recipe exists
- **When** `deleteRecipe(name)` opens the card's "…" menu, clicks the `Delete` menuitem, then confirms in the dialog (button matched by `/^delete$/i`)
- **Then** `expectRecipeNotVisible(name)` — no `role="listitem"` with that name is visible within 5 s
- **API**: `DELETE /api/v1/recipes/{id}`

## Acceptance

Recipes CRUD works end-to-end with computed nutrition per serving and ingredient persistence.

### `mixed-unit ingredients, instructions, and per-serving nutrition survive edits`

- Creates a liquid product with `ml`, density `1`, and fiber `7.5 g`; verifies its detail after reload.
- Creates a two-serving recipe with `100 g` of a `100 kcal/100 g` product and `200 ml` of a `50 kcal/100 g` product, plus two instruction lines.
- Verifies both ingredients, units, instructions, and `100 kcal` per serving after reload.
- Edits the named gram ingredient to `200 g` and servings to four; verifies persisted amounts, instructions, and `75 kcal` per serving after reload.
- Ingredient edit helper matches product name because persisted ingredient order can differ from creation order.

## Gaps

- Ingredient amount edge cases (zero, very large)
- Prep-time validation (out-of-range values)
- `detail-improvements.spec.ts` exercises the detail-page scaling control;
  exact scaling math remains a unit/component-test concern
- Adding/removing ingredients on edit
- Search by ingredient name (not just recipe name)
