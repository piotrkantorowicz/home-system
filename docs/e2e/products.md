# products.spec.ts — Products

**Purpose**: products CRUD and search.

**Setup**: each test creates a uniquely-named product (`Date.now()` suffix); names are tracked for teardown.

## Tests

- `user can create a product with nutrition values` — calories/protein/carbs/fat all saved, product appears in list
- `user can search for products by name` — search box filters; either results or empty state visible
- `product form shows required-field error when name is empty` — client validation
- `user can edit an existing product` — edit calorie value; detail page reflects the new value
- `user can delete a product and it disappears from the list` — destructive action confirms

## Acceptance

Products CRUD surface works with form validation.

## Gaps

- Macro percentage validation
- Server-side error display (e.g., 409 conflict on duplicate name)
- Unit selector (g / ml / oz)
- Import-from-recipe
- Bulk operations
