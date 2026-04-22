# recipes.spec.ts — Recipes CRUD

`test.describe.configure({ mode: 'serial' })`

**Purpose**: recipe CRUD with at least one ingredient, plus reading nutrition-per-serving on the detail view.

**Setup**: first test creates an ingredient product and a recipe referencing it; subsequent tests reuse them.

## Tests

- `user can create a recipe with an ingredient` — recipe appears in list
- `user can view recipe details including the ingredient` — detail view shows nutrition per serving + ingredient name
- `user can edit a recipe to update the number of servings` — servings change to 4, detail reflects it
- `user can delete a recipe and it disappears from the list`

## Acceptance

Recipes CRUD works end-to-end with computed nutrition per serving.

## Gaps

- Multi-ingredient recipes
- Ingredient amount / unit edge cases
- Instructions field
- Prep-time validation
- Dietary tags
- Recipe scaling math
