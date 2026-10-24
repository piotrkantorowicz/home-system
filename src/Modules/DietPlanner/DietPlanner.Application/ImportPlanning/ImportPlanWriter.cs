namespace DietPlanner.Application.ImportPlanning;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Stages everything an error-free <see cref="ResolvedImport"/> describes: new products, new recipes,
/// the schedule changes legacy mode asked for, and planned meals. The caller commits once, so the
/// whole import lands in one transaction. The only write path for imports.
/// </summary>
internal sealed class ImportPlanWriter(
    IProductRepository productRepository,
    IRecipeRepository recipeRepository,
    IMealEntryRepository mealEntryRepository,
    IMealScheduleConfigRepository scheduleRepository)
{
    /// <summary>What writing <paramref name="resolved"/> creates and reuses, known before any write.</summary>
    public static ImportStatsDto StatsOf(ResolvedImport resolved)
        => new(
            ProductsCreated: resolved.Products.Count(p => p.IsNew),
            ProductsReused: resolved.Products.Count(p => !p.IsNew),
            RecipesCreated: resolved.Recipes.Count(r => r.IsNew),
            RecipesReused: resolved.Recipes.Count(r => !r.IsNew),
            MealEntriesCreated: resolved.Meals.Count);

    public async Task StageAsync(ResolvedImport resolved, string userId, Guid personId, DateTime now, CancellationToken ct)
    {
        if (resolved.HasErrors)
            throw new InvalidOperationException("Only an error-free resolution can be written.");

        foreach (var p in resolved.Products.Where(p => p.IsNew))
        {
            var d = p.Definition;
            var nutrition = new NutritionPer100g(d.CaloriesPer100g, d.ProteinPer100g, d.CarbsPer100g, d.FatPer100g, d.FiberPer100g);
            await productRepository.AddAsync(
                Product.Create(p.Id, d.Name, nutrition, d.Unit ?? "g", d.DensityGramsPerMl, d.GramPerPiece, userId, now), ct);
        }

        foreach (var r in resolved.Recipes.Where(r => r.IsNew))
        {
            var d = r.Definition;
            var recipe = Recipe.Create(r.Id, d.Name, d.Description, d.Instructions, d.Servings ?? 1, d.PrepTimeMinutes, userId, now);
            foreach (var ing in r.Ingredients)
                recipe.AddIngredient(RecipeIngredientId.New(), ing.ProductId, ing.Amount, ing.Unit);
            await recipeRepository.AddAsync(recipe, ct);
        }

        var slotIds = await ResolveSlotIdsAsync(resolved, personId, now, ct);

        foreach (var meal in resolved.Meals)
        {
            var entry = MealEntry.Create(
                MealEntryId.New(),
                personId,
                meal.Date,
                slotIds[meal.SlotName],
                meal.RecipeId,
                meal.Servings,
                meal.Notes,
                mealTime: null,
                sequenceOrder: null,
                now);
            await mealEntryRepository.AddAsync(entry, ct);
        }
    }

    /// <summary>Maps slot names to ids, creating the default schedule and "Other" slot when legacy resolution asked for them.</summary>
    private async Task<Dictionary<string, MealSlotId>> ResolveSlotIdsAsync(
        ResolvedImport resolved, Guid personId, DateTime now, CancellationToken ct)
    {
        if (resolved.Meals.Count == 0)
            return [];

        var schedule = resolved.Schedule;
        if (schedule is null)
        {
            schedule = MealScheduleConfig.Create(MealScheduleConfigId.New(), personId, ImportPlanResolver.DefaultMealSlots, now);
            await scheduleRepository.AddAsync(schedule, ct);
        }

        if (resolved.CreatesOtherSlot)
        {
            // Append via the diff API so existing slot identities are preserved.
            var upserts = schedule.Slots
                .OrderBy(s => s.SortOrder)
                .Select(s => new MealSlotUpsert(s.Id, s.Name, s.DefaultTime))
                .Append(new MealSlotUpsert(null, ImportPlanResolver.OtherSlot, new TimeOnly(12, 0)))
                .ToList();
            schedule.ApplyUpdate(upserts, now);
        }

        return schedule.Slots
            .OrderBy(s => s.SortOrder)
            .DistinctBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(s => s.Name, s => s.Id, StringComparer.OrdinalIgnoreCase);
    }
}
