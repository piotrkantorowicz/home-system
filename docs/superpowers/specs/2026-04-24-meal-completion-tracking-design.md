# Meal Completion Tracking — Design

**Issue:** [#21](https://github.com/piotrkantorowicz/home-system/issues/21)
**Date:** 2026-04-24

---

## 1. Context

The Calendar shows planned meals but has no feedback loop for what was actually eaten. Users want to confirm meals went to plan, override individual meals when they ate something different, and quickly mark a whole day as eaten. The change is additive on top of the existing `MealEntry` aggregate (now living on `MealSlotId` after #144).

---

## 2. Decisions

| # | Decision | Rationale |
|---|---|---|
| 1 | Override allows BOTH a replacement recipe AND ad-hoc products in one record | Models reality (e.g. "had soup AND a chocolate bar") with marginal data-model cost; subsumes the recipe-only and product-only cases. |
| 2 | Daily nutrition uses *actual* macros for `Modified` entries; *planned* macros for `Done` and `Planned` | Preserves today's behaviour for users who don't engage with completion (no silent regressions); makes overrides immediately visible in the tracker. |
| 3 | Three single-purpose action endpoints (complete / override / reset) instead of one permissive `/status` PUT | Each endpoint has clear semantics, validation is easy, audit logging straightforward. |
| 4 | Bulk "mark all done for today" is included in this scope | Most common confirmation pattern; cheap to add now, awkward to retro-fit later. |

---

## 3. Architecture

```
DietPlanner module (existing)
├── Domain
│   ├── Aggregates/MealEntry.cs              [+ Status, ActualRecipeId, _actualProducts; MarkDone/ApplyOverride/Reset]
│   ├── Entities/MealEntryActualProduct.cs   [NEW]
│   └── ValueObjects/
│       ├── MealEntryStatus.cs               [NEW]
│       └── MealEntryActualProductId.cs      [NEW]
├── Application
│   ├── Commands/
│   │   ├── CompleteMealEntry/               [NEW]
│   │   ├── OverrideMealEntry/               [NEW]
│   │   ├── ResetMealEntry/                  [NEW]
│   │   └── BulkCompleteMealEntries/         [NEW]
│   └── Queries/
│       ├── GetMealEntries/                  [+ status, actualRecipe, actualProducts]
│       └── GetNutritionSummary/             [+ branch on status: actual vs planned]
├── Infrastructure
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── MealEntryConfiguration.cs                [+ status, actual_recipe_id, owned collection]
│   │   │   └── MealEntryActualProductConfiguration.cs   [NEW]
│   │   └── Repositories/MealEntryRepository.cs          [+ Include _actualProducts]
│   └── Migrations/<timestamp>_AddMealEntryCompletion.cs [NEW]
└── Api
    └── MealEndpoints.cs                                 [+ complete / override / reset / bulk-complete]

src/ui/src/modules/diet-planner
├── api/hooks/
│   └── useMeals.ts                          [+ useCompleteMeal, useOverrideMeal, useResetMeal, useBulkCompleteMeals]
├── components/
│   └── diet-plans/
│       ├── MealStatusBadge.tsx              [NEW]
│       └── MealOverrideDialog.tsx           [NEW]
├── pages/Calendar.tsx                       [+ status badge, status menu, bulk-complete button per day]
└── locales/{en,pl}.json                     [+ calendar.meal_status.*, calendar.meal_actions.*, override_dialog.*]
```

---

## 4. Backend

### 4.1 Domain

**`MealEntryStatus.cs`** — `enum { Planned = 0, Done = 1, Modified = 2 }`

**`MealEntryActualProductId.cs`** — typed id matching the project pattern.

**`MealEntryActualProduct.cs`** (entity, internal `Create`):

```
Id: MealEntryActualProductId
ProductId: ProductId
Amount: decimal
Unit: string
```

Validation: `amount > 0`; `unit` non-empty.

**`MealEntry.cs`** additions:

```
Status: MealEntryStatus              (default Planned)
ActualRecipeId: RecipeId?
IReadOnlyCollection<MealEntryActualProduct> ActualProducts
```

Methods:

- `MarkDone()` — throws `DietPlannerDomainException` if `Status == Modified` ("Reset the override before marking done."); idempotent on `Done`. Sets `Status = Done`.
- `ApplyOverride(RecipeId? actualRecipeId, IReadOnlyList<(ProductId, decimal Amount, string Unit)> actualProducts)` — throws `DietPlannerDomainException` if `actualRecipeId is null && actualProducts.Count == 0` ("Override must include a recipe or at least one product."). Replaces `ActualRecipeId`, clears and re-populates the children collection, sets `Status = Modified`.
- `Reset()` — sets `ActualRecipeId = null`, clears `_actualProducts`, sets `Status = Planned`. Idempotent on `Planned`.

The collection is exposed as `IReadOnlyCollection<MealEntryActualProduct>` and mutated only through `ApplyOverride` / `Reset`.

### 4.2 Application

**`CompleteMealEntry/`** — command `(Guid Id, string UserId)` + handler:

1. Load entry by id; throw `NotFoundException` if missing.
2. Ownership check (`entry.UserId == command.UserId`); throw `NotFoundException` if not.
3. `entry.MarkDone()`; `_unitOfWork.CommitAsync(ct)`.

**`OverrideMealEntry/`** — command `(Guid Id, string UserId, Guid? ActualRecipeId, IReadOnlyList<ActualProductInput> ActualProducts)` where `ActualProductInput(Guid ProductId, decimal Amount, string Unit)`. Validator: at least one of `ActualRecipeId != null` or `ActualProducts.Count > 0`; for each product `Amount > 0` and `Unit` non-blank.

Handler:

1. Load entry; ownership check.
2. If `ActualRecipeId is not null` → load via `IRecipeRepository`; throw `NotFoundException` if missing or `recipe.CreatedByUserId != command.UserId`.
3. For each `ActualProducts[i].ProductId` → load via `IProductRepository.GetByIdsAsync` (single `IN (...)` query); throw `NotFoundException` if any missing or not owned by user.
4. `entry.ApplyOverride(...)`; commit.

**`ResetMealEntry/`** — command `(Guid Id, string UserId)` + handler: load, ownership check, `entry.Reset()`, commit.

**`BulkCompleteMealEntries/`** — command `(string UserId, DateOnly Date)` returning `BulkCompleteResult(int Completed)`. Validator: `Date <= DateOnly.FromDateTime(DateTime.UtcNow)`.

Handler:

1. Query `_repository.GetByUserAndDateRangeAsync(userId, date, date, ct)`.
2. Filter to `Status == Planned`. (Skips `Done`: idempotent. Skips `Modified`: intentional overrides.)
3. For each → `entry.MarkDone()`. Count.
4. `_unitOfWork.CommitAsync(ct)`. Return `BulkCompleteResult(count)`.

**`GetMealEntries/MealEntryDto`** — extend:

```
status: string                         // "Planned" | "Done" | "Modified"
actualRecipe: { id: Guid; name: string }?
actualProducts: [{ id: Guid; productId: Guid; productName: string; amount: decimal; unit: string }]
```

Handler joins `Recipes` again on `MealEntries.ActualRecipeId` (left join), and joins `MealEntryActualProducts` + `Products` for the children. Single query; ordered by `Date, SlotSortOrder, SequenceOrder`.

**`GetNutritionSummary/`** — current handler aggregates per-day macros from each entry's recipe (`servings * recipe.macroPer1`). Update:

- For each entry with `Status == Modified`:
  - If `ActualRecipeId != null` → use that recipe's macros × `Servings`
  - Plus, for each `ActualProducts[i]` → derive macros from `Product.NutritionPer100g × (amount in grams / 100)`. The `unit` → grams conversion uses the existing `Product.DensityGramsPerMl` / `GramPerPiece` rules already used by `NutritionCalculator`. Reuse that service. If `INutritionCalculator` doesn't already expose a `MacrosFor(Product, decimal amount, string unit)` method, add it as part of this work — pure addition, no breaking change.
- For all other statuses → unchanged (planned recipe × servings).

This means the summary handler now needs `INutritionCalculator` injected. Refactor the existing per-recipe math into the calculator if it isn't already there.

### 4.3 Infrastructure

**`MealEntryConfiguration.cs`** — additions:

```csharp
builder.Property(x => x.Status)
    .HasConversion<string>()
    .HasMaxLength(32)
    .HasColumnName("status");

builder.Property(x => x.ActualRecipeId)
    .HasConversion(id => id == null ? (Guid?)null : id.Value,
                   value => value == null ? null : RecipeId.From(value.Value))
    .HasColumnName("actual_recipe_id");

builder.HasOne<Recipe>()
    .WithMany()
    .HasForeignKey(x => x.ActualRecipeId)
    .OnDelete(DeleteBehavior.Restrict);

builder.HasMany(x => x.ActualProducts)
    .WithOne()
    .HasForeignKey("meal_entry_id")
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(x => x.ActualProducts).UsePropertyAccessMode(PropertyAccessMode.Field);
```

**`MealEntryActualProductConfiguration.cs`** — `meal_entry_actual_products` table:

- `id` uuid pk
- `meal_entry_id` uuid not null (FK above, cascade)
- `product_id` uuid not null (FK to `products.id`, **Restrict**)
- `amount` numeric(10,3) not null
- `unit` varchar(32) not null
- Index on `(meal_entry_id)`

**`MealEntryRepository.GetByIdAsync`** — `.Include("_actualProducts")` so `ApplyOverride` / `Reset` see the current children.

**Migration `AddMealEntryCompletion`**:

- `ALTER TABLE meal_entries ADD COLUMN status varchar(32) NOT NULL DEFAULT 'Planned';`
- `ALTER TABLE meal_entries ADD COLUMN actual_recipe_id uuid NULL;`
- `ADD CONSTRAINT FK_meal_entries_recipes_actual_recipe_id FOREIGN KEY (actual_recipe_id) REFERENCES recipes (id) ON DELETE RESTRICT;`
- Create `meal_entry_actual_products` table.
- Down: drop the new table, drop FK, drop columns.

### 4.4 API

`MealEndpoints` adds four routes mounted on the existing `/api/v1/meals` group:

| Method | Path | Returns | Codes |
|---|---|---|---|
| PATCH | `/api/v1/meals/{id:guid}/complete` | 204 | 400, 404, 422, 401 |
| PATCH | `/api/v1/meals/{id:guid}/override` | 204 | 400, 404, 422, 401 |
| PATCH | `/api/v1/meals/{id:guid}/reset` | 204 | 404, 401 |
| POST | `/api/v1/meals/bulk-complete` | 200 + `{ completed: int }` | 400, 401 |

Request records:

```csharp
public sealed record OverrideMealEntryRequest(
    Guid? ActualRecipeId,
    IReadOnlyList<ActualProductRequest> ActualProducts);

public sealed record ActualProductRequest(Guid ProductId, decimal Amount, string Unit);

public sealed record BulkCompleteMealsRequest(DateOnly Date);

public sealed record BulkCompleteMealsResponse(int Completed);
```

`MealEntryDto` already extended in 4.2 — automatically reflected in OpenAPI.

### 4.5 Tests

**Unit (`DietPlanner.UnitTests`):**

- `Domain/MealEntryCompletionTests` — MarkDone happy path; MarkDone-on-Done is no-op; MarkDone-on-Modified throws; ApplyOverride sets state and Status; ApplyOverride with empty inputs throws; ApplyOverride replaces previous override (children cleared); Reset clears state and resets Status; Reset on Planned no-op
- `Application/CompleteMealEntryCommandHandlerTests`
- `Application/OverrideMealEntryCommandHandlerTests` — happy path with recipe-only, products-only, both; 404 on missing entry; 404 on bogus actualRecipeId; 404 on bogus productId
- `Application/ResetMealEntryCommandHandlerTests`
- `Application/BulkCompleteMealEntriesCommandHandlerTests` — completes only Planned, leaves Modified alone, returns count, no-op when no Planned entries

**Integration (`DietPlanner.IntegrationTests`):**

- `MealEntryCompletionEndpointsTests`:
  - PATCH complete happy path → 204 + GET shows `Done`
  - PATCH complete on Modified → 422
  - PATCH override with both recipe + products → 204 + GET shows actual data
  - PATCH override with empty body → 400
  - PATCH override with foreign user's product → 404
  - PATCH reset clears all override state
  - POST bulk-complete on a date with mix of Planned/Done/Modified → returns count of Planned only; subsequent GET shows them as Done; Modified untouched
  - POST bulk-complete with future date → 400
  - 401 on every endpoint without auth
- `GetNutritionSummaryWithOverridesTests` — Modified entries contribute actual macros; Done/Planned contribute planned macros

---

## 5. Frontend

### 5.1 Type generation

After backend is up: `npm run generate:api:diet-planner`. New `MealEntryStatus`, extended `MealEntryDto`, new request DTOs.

### 5.2 API hooks (`api/hooks/useMeals.ts`)

```ts
export function useCompleteMeal()       // PATCH /{id}/complete
export function useOverrideMeal()       // PATCH /{id}/override
export function useResetMeal()          // PATCH /{id}/reset
export function useBulkCompleteMeals()  // POST  /bulk-complete → returns { completed }
```

All four invalidate `queryKeys.meals.all()` and `queryKeys.nutritionSummary.all()`.

### 5.3 Components

**`MealStatusBadge.tsx`** — small inline badge:

| Status | Visual | Icon |
|---|---|---|
| `Planned` | muted ring, default colour | `Clock` |
| `Done` | green ring + green tint | `CheckCircle` |
| `Modified` | amber ring + amber tint | `Pencil` |

A11y: `aria-label` is the translated status string.

**`MealOverrideDialog.tsx`** — opened from a meal-card menu. Two collapsible sections:

1. **Replace recipe** — `<RecipeSearch>` autocomplete (reuses the existing recipe lookup pattern from `MealForm`). Single value.
2. **Add ad-hoc products** — repeating row: `<ProductSearch>` + amount input + unit select. "+ Add product" button. Remove (`X`) per row.

Submit disabled until at least one section has data. On success → close + toast.

### 5.4 Calendar integration (`pages/Calendar.tsx`)

Per meal card:

- `<MealStatusBadge status={meal.status} />` on the leading edge of the title row
- 3-dot menu with: ✓ Mark done, ✏ Override, ↺ Reset to planned. Reset hidden when status is Planned.
- When `status === 'Modified'`: card title shows `actualRecipe.name ?? actualProducts[0].productName + (rest.length > 0 ? ` + ${rest.length} more` : '')`. The original planned recipe name appears below in muted text with strike-through.

Per day-card header:

- "Mark all done" small ghost button next to the date. Visible only when `dayMeals.some(m => m.status === 'Planned')`. On click: `useBulkCompleteMeals.mutate({ date: dateStr })`. Toast: `t('calendar.bulk_complete.success', { count: result.completed })`. Suppress toast when `count === 0`.

### 5.5 i18n

EN + PL keys:

```
calendar.meal_status.{planned,done,modified}
calendar.meal_actions.{mark_done,override,reset}
calendar.meal_action_success.{done,override,reset}
calendar.meal_action_error.{done,override,reset}
calendar.bulk_complete.button
calendar.bulk_complete.success           // "{{count}} meals marked as done"
calendar.bulk_complete.error
override_dialog.title
override_dialog.replace_recipe.heading
override_dialog.replace_recipe.placeholder
override_dialog.add_products.heading
override_dialog.add_products.add_button
override_dialog.product
override_dialog.amount
override_dialog.unit
override_dialog.submit
override_dialog.cancel
override_dialog.validation.empty
```

### 5.6 Tests

- `MealStatusBadge.test.tsx` — renders correct icon + colour per status, has correct `aria-label`
- `MealOverrideDialog.test.tsx` — submit disabled until one section filled; recipe-only submit; products-only submit; combined submit; remove product row
- `useMeals.test.ts` — extend with the four new mutation hook tests (happy + error)
- `calendarToast.test.tsx` — mark-done toast, override toast, reset toast, bulk-complete toast (count message), bulk-complete with 0 suppresses toast

---

## 6. Error handling & edge cases

### Backend mapping

| Error | Status | Source |
|---|---|---|
| Validator failure (empty override, non-positive amount, future date for bulk) | 400 | `ValidationCommandDispatcherDecorator` |
| `MarkDone` while `Modified` | 422 | `DietPlannerDomainException` via middleware |
| Override with empty recipe + empty products | 422 | Domain throws (validator catches first; this is the second-line guard) |
| Entry not found / not owned | 404 | Endpoint / handler |
| `ActualRecipeId` not found / not owned | 404 | Handler |
| `ActualProductId` not found / not owned | 404 | Handler |
| Missing/invalid auth | 401 | `RequireAuthorization()` |

### Edge cases

| Scenario | Behaviour |
|---|---|
| Mark Done on already-Done meal | Idempotent: 204, no state change |
| Reset on already-Planned meal | Idempotent: 204 |
| Override an already-Modified meal | Replaces all override state (children deleted, re-inserted) |
| Override an already-Done meal | Allowed — transitions Status from Done to Modified |
| Override with same recipe as planned | Allowed; status flips to Modified anyway. No value in blocking it. |
| Delete a Recipe referenced as `ActualRecipeId` | Blocked by FK Restrict |
| Delete a Product referenced in `ActualProducts` | Blocked by FK Restrict |
| Delete a meal slot with Done/Modified entries | Blocked by the existing slot-deletion guard from #144 (entries-exist check ignores status) |
| `GetNutritionSummary` for a date with mixed statuses | Per-row branch: Modified uses actual; others use planned |
| Bulk complete during a race (entry transitions to Modified between query and mark) | Handler re-checks `Status == Planned` before calling `MarkDone()`; skips if changed |
| Bulk complete with 0 Planned entries | Returns `{ completed: 0 }`; frontend suppresses toast |

---

## 7. Out of scope

- Nutrition diff (planned vs actual side-by-side) — deferred per issue
- Recurring completion patterns — deferred per issue
- Bulk override / bulk reset — only bulk *complete* is in scope; override and reset are deliberate single-meal actions
- Editing an override partially — overrides are replaced wholesale
- Marking a meal Done in the past — allowed (no date restriction on the per-meal endpoint); only the bulk endpoint disallows future dates

---

## 8. Acceptance criteria

- [x] User can one-click Mark done on a planned meal — `PATCH /{id}/complete`
- [x] User can override a meal with a different recipe and/or ad-hoc products — `PATCH /{id}/override`
- [x] User can revert a Done or Modified meal back to Planned — `PATCH /{id}/reset`
- [x] User can mark all of today's planned meals done in one click — `POST /bulk-complete`
- [x] Calendar visually differentiates Planned / Done / Modified
- [x] Daily nutrition summary uses actual intake for Modified entries
- [x] All migrations preserve existing entries (default `Status = Planned`, no data loss)
