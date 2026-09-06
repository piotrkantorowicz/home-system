---
name: pr-summary
description: "Generate a concise PR body summary from diff vs main"
---

# pr-summary

Generate a concise PR body summary for the changes on the current branch vs `main`.

Steps:
1. Run `git diff main...HEAD --stat` to see which files changed.
2. Run `git log main..HEAD --oneline` to see the commits.
3. Run `git diff main...HEAD` to read the full diff.
4. Analyse the diff and produce a summary using **exactly** the format below.

## Format rules

- Group changes by area using bold headers, e.g. `**Backend:**`, `**Frontend — Feature Name:**`
- Each item is a short bullet starting with an imperative verb (Add, New, Fix, Remove, Update, Replace…)
- Be specific: name files, endpoints, DTOs, components, hooks, keys
- No filler phrases ("in order to", "so that", "this allows")
- Keep each bullet to 1–2 lines max`
- Do NOT include a top-level title or PR number

## Example output

```
Backend:
- Add GET /api/v1/meals/nutrition-summary?from=&to= endpoint returning per-day nutrition totals
- Add DailyNutritionDto (date, calories, protein, carbs, fat, fiber) to MealDtos.cs
- Add GetNutritionSummaryAsync to IMealService / MealService — groups meal entries by date,
  uses NutritionCalculator.CalculateNutritionForServings per entry

Frontend — Nutrition Summary page:
- New /diet-planner/nutrition page with date range picker (from/to inputs + Apply button,
  defaults to current week)
- Totals card and daily average card for the selected range
- Goal progress panel — MacroProgressBar for each macro comparing range total vs daily goal
  × days in range (only shown when goals are configured)
- Daily breakdown table with client-side pagination
- useNutritionSummary hook added to useMeals.ts
- nutrition_summary and nutrition_page i18n keys (en + pl)

Frontend — Shared Pagination component:
- New Pagination component in shared/components/ui/ with prev/next buttons and a
  "Rows per page" dropdown (10 / 25 / 50 / 100, default 25)
- ProductList and RecipeList — replaced inline pagination blocks with the shared component;
  page size is now user-controlled (was hardcoded to 50)
- common.rows_per_page i18n key added (en + pl)
```

Now generate the summary for the current branch.
