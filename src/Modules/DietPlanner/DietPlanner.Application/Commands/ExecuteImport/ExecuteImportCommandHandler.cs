namespace DietPlanner.Application.Commands.ExecuteImport;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ExecuteImportCommandHandler(
    IProductRepository productRepository,
    IRecipeRepository recipeRepository,
    IMealEntryRepository mealEntryRepository,
    IMealScheduleConfigRepository scheduleRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ExecuteImportCommand, ImportResultDto>
{
    private static readonly IReadOnlyList<(string Name, TimeOnly DefaultTime)> DefaultMealSlots =
    [
        ("Breakfast", new TimeOnly(7, 0)),
        ("Lunch", new TimeOnly(12, 0)),
        ("Dinner", new TimeOnly(18, 0)),
        ("Snack", new TimeOnly(15, 0)),
    ];

    public async Task<ImportResultDto> HandleAsync(ExecuteImportCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var import = command.Import;
        var userId = command.UserId;

        int productsCreated = 0, productsReused = 0;
        int recipesCreated = 0, recipesReused = 0;
        int mealEntriesCreated = 0;

        // ── 1. Upsert products ──────────────────────────────────────────────
        // Build a name → ProductId map for recipe ingredient resolution
        var productIdByName = new Dictionary<string, ProductId>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in import.Products ?? [])
        {
            if (string.IsNullOrWhiteSpace(p.Name))
                continue;

            var existing = await productRepository.GetByNameAsync(p.Name, userId, ct);
            if (existing is not null)
            {
                productIdByName[p.Name] = existing.Id;
                productsReused++;
            }
            else
            {
                var id = ProductId.New();
                var nutrition = new NutritionPer100g(p.CaloriesPer100g, p.ProteinPer100g, p.CarbsPer100g, p.FatPer100g, p.FiberPer100g);
                var product = Product.Create(id, p.Name, nutrition, p.Unit ?? "g", p.DensityGramsPerMl, p.GramPerPiece, userId, now);
                await productRepository.AddAsync(product, ct);
                productIdByName[p.Name] = id;
                productsCreated++;
            }
        }

        // ── 2. Upsert recipes ───────────────────────────────────────────────
        var recipeIdByName = new Dictionary<string, RecipeId>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in import.Recipes ?? [])
        {
            if (string.IsNullOrWhiteSpace(r.Name))
                continue;

            var existing = await recipeRepository.GetByNameAsync(r.Name, userId, ct);
            if (existing is not null)
            {
                recipeIdByName[r.Name] = existing.Id;
                recipesReused++;
            }
            else
            {
                var id = RecipeId.New();
                var recipe = Recipe.Create(id, r.Name, r.Description, r.Instructions,
                    r.Servings ?? 1, r.PrepTimeMinutes, userId, now);

                foreach (var ing in r.Ingredients ?? [])
                {
                    if (string.IsNullOrWhiteSpace(ing.Product))
                        continue;

                    // Resolve product ID — first from this import batch, then from DB
                    if (!productIdByName.TryGetValue(ing.Product, out ProductId? productId))
                    {
                        var dbProduct = await productRepository.GetByNameAsync(ing.Product, userId, ct);
                        if (dbProduct is null)
                            continue; // Skip unresolvable ingredients
                        productId = dbProduct.Id;
                    }

                    recipe.AddIngredient(RecipeIngredientId.New(), productId, ing.Amount ?? 100m, ing.Unit);
                }

                await recipeRepository.AddAsync(recipe, ct);
                recipeIdByName[r.Name] = id;
                recipesCreated++;
            }
        }

        // ── 3. Resolve meal schedule (auto-provision default if missing) ────
        var schedule = await scheduleRepository.GetByPersonIdAsync(command.PersonId, ct);
        if (schedule is null)
        {
            schedule = MealScheduleConfig.Create(MealScheduleConfigId.New(), command.PersonId, DefaultMealSlots, now);
            await scheduleRepository.AddAsync(schedule, ct);
        }

        // ── 4. Create meal entries ──────────────────────────────────────────
        foreach (var day in import.Schedule ?? [])
        {
            if (!DateOnly.TryParse(day.Date, out DateOnly date))
                continue;

            foreach (var meal in day.Meals ?? [])
            {
                if (string.IsNullOrWhiteSpace(meal.Recipe))
                    continue;

                // Resolve recipe ID — from this import batch or DB
                if (!recipeIdByName.TryGetValue(meal.Recipe, out RecipeId? recipeId))
                {
                    var dbRecipe = await recipeRepository.GetByNameAsync(meal.Recipe, userId, ct);
                    if (dbRecipe is null)
                        continue; // Skip unresolvable meals
                    recipeId = dbRecipe.Id;
                }

                MealSlotId mealSlotId = ResolveMealSlot(schedule, meal.Type, now);

                var entry = MealEntry.Create(
                    MealEntryId.New(),
                    command.PersonId,
                    date,
                    mealSlotId,
                    recipeId,
                    meal.Servings ?? 1m,
                    meal.Notes,
                    mealTime: null,
                    sequenceOrder: null,
                    now);

                await mealEntryRepository.AddAsync(entry, ct);
                mealEntriesCreated++;
            }
        }

        await unitOfWork.CommitAsync(ct);

        return new ImportResultDto(
            Message: $"Import completed successfully.",
            Stats: new ImportStatsDto(
                ProductsCreated: productsCreated,
                ProductsReused: productsReused,
                RecipesCreated: recipesCreated,
                RecipesReused: recipesReused,
                MealEntriesCreated: mealEntriesCreated));
    }

    private static MealSlotId ResolveMealSlot(MealScheduleConfig schedule, string? rawType, DateTime now)
    {
        if (!string.IsNullOrWhiteSpace(rawType))
        {
            var match = schedule.Slots.FirstOrDefault(
                s => string.Equals(s.Name, rawType, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match.Id;
        }

        // Fallback: dump into "Other"; create the slot if it doesn't exist yet.
        var other = schedule.Slots.FirstOrDefault(
            s => string.Equals(s.Name, "Other", StringComparison.OrdinalIgnoreCase));

        if (other is null)
        {
            // Append "Other" via the diff API so identity-preservation is honoured.
            var upserts = schedule.Slots
                .OrderBy(s => s.SortOrder)
                .Select(s => new MealSlotUpsert(s.Id, s.Name, s.DefaultTime))
                .Append(new MealSlotUpsert(null, "Other", new TimeOnly(12, 0)))
                .ToList();
            schedule.ApplyUpdate(upserts, now);
            other = schedule.Slots.First(s => string.Equals(s.Name, "Other", StringComparison.OrdinalIgnoreCase));
        }

        return other.Id;
    }
}
