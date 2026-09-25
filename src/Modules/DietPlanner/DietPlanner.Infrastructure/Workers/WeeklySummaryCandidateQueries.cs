namespace DietPlanner.Infrastructure.Workers;

using DietPlanner.Application.Workers;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class WeeklySummaryCandidateQueries(DietPlannerDbContext dbContext)
    : IWeeklySummaryCandidateQueries
{
    private const string DefaultLocale = "en";
    private const decimal MlPerLiter = 1000m;
    private const int DaysInWeek = 7;

    public async Task<IReadOnlyList<WeeklySummaryCandidate>> GetCandidatesAsync(CancellationToken ct)
    {
        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.WeeklySummaryEnabled
            join state in dbContext.WeeklySummaryStates.AsNoTracking()
                on settings.PersonId equals state.PersonId into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            select new
            {
                settings.PersonId,
                settings.WeeklySummaryDayOfWeekUtc,
                settings.WeeklySummaryTimeOfDayUtc,
                LastAt = state == null ? (DateTime?)null : state.LastWeeklySummaryAt
            }).ToListAsync(ct);

        return rows
            .Select(r => new WeeklySummaryCandidate(
                r.PersonId,
                DefaultLocale,
                r.WeeklySummaryDayOfWeekUtc,
                r.WeeklySummaryTimeOfDayUtc,
                r.LastAt))
            .ToList();
    }

    public async Task<WeeklyStats> GetStatsAsync(
        Guid personId, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct)
    {
        int targetKcal = await ComputeTargetKcalAsync(personId, ct);
        KcalResult kcalResult = await ComputeTotalKcalAndCountsAsync(personId, weekStart, weekEnd, ct);
        decimal avgWaterLiters = await ComputeAvgWaterLitersAsync(personId, weekStart, weekEnd, ct);
        decimal? weightDeltaKg = await ComputeWeightDeltaAsync(personId, weekStart, weekEnd, ct);

        return new WeeklyStats(
            TotalKcal: kcalResult.TotalKcal,
            TargetKcal: targetKcal,
            AvgWaterLiters: avgWaterLiters,
            WeightDeltaKg: weightDeltaKg,
            MealsCompleted: kcalResult.MealsCompleted,
            MealsPlanned: kcalResult.MealsPlanned);
    }

    private async Task<int> ComputeTargetKcalAsync(Guid personId, CancellationToken ct)
    {
        int? goal = await dbContext.UserGoals
            .AsNoTracking()
            .Where(g => g.PersonId == personId)
            .Select(g => g.DailyCalorieTarget)
            .FirstOrDefaultAsync(ct);

        return (goal ?? 0) * DaysInWeek;
    }

    private async Task<decimal> ComputeAvgWaterLitersAsync(
        Guid personId, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct)
    {
        int totalMl = await dbContext.WaterIntakes
            .AsNoTracking()
            .Where(w => w.PersonId == personId
                && w.Date >= weekStart
                && w.Date <= weekEnd)
            .SumAsync(w => w.AmountMl, ct);

        return Math.Round(totalMl / MlPerLiter / DaysInWeek, 2);
    }

    private async Task<decimal?> ComputeWeightDeltaAsync(
        Guid personId, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct)
    {
        List<decimal> weights = await dbContext.WeightEntries
            .AsNoTracking()
            .Where(w => w.PersonId == personId
                && w.Date >= weekStart
                && w.Date <= weekEnd)
            .OrderBy(w => w.Date)
            .Select(w => w.WeightKg)
            .ToListAsync(ct);

        if (weights.Count < 2) return null;

        return Math.Round(weights[weights.Count - 1] - weights[0], 2);
    }

    private async Task<KcalResult> ComputeTotalKcalAndCountsAsync(
        Guid personId, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct)
    {
        List<EntryProjection> entries = await dbContext.MealEntries
            .AsNoTracking()
            .Where(me => me.PersonId == personId
                && me.Date >= weekStart
                && me.Date <= weekEnd)
            .Select(me => new EntryProjection
            {
                Servings = me.Servings,
                Status = me.Status,
                PlannedRecipeId = me.RecipeId,
                ActualRecipeId = me.ActualRecipeId,
                ActualProducts = me.ActualProducts
                    .Select(ap => new ActualProductProjection
                    {
                        ProductId = ap.ProductId,
                        Amount = ap.Amount,
                        Unit = ap.Unit
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        int mealsPlanned = entries.Count;
        int mealsCompleted = entries.Count(e =>
            e.Status == MealEntryStatus.Done || e.Status == MealEntryStatus.Modified);

        if (entries.Count == 0) return new KcalResult(0, mealsPlanned, mealsCompleted);

        List<RecipeId> recipeIds = entries
            .Select(EffectiveRecipeId)
            .Where(id => id is not null)
            .Select(id => id!)
            .Distinct()
            .ToList();

        Dictionary<RecipeId, RecipeProjection> recipes = await dbContext.Recipes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => recipeIds.Contains(r.Id))
            .Select(r => new RecipeProjection
            {
                Id = r.Id,
                Servings = r.Servings,
                Ingredients = r.Ingredients
                    .Select(i => new IngredientProjection
                    {
                        ProductId = i.ProductId,
                        Amount = i.Amount,
                        Unit = i.Unit
                    })
                    .ToList()
            })
            .ToDictionaryAsync(r => r.Id, ct);

        List<ProductId> productIds = recipes.Values
            .SelectMany(r => r.Ingredients.Select(i => i.ProductId))
            .Concat(entries.SelectMany(e => e.ActualProducts.Select(ap => ap.ProductId)))
            .Distinct()
            .ToList();

        Dictionary<ProductId, ProductProjection> products = await dbContext.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductProjection
            {
                Id = p.Id,
                Calories = p.Nutrition.Calories,
                DensityGramsPerMl = p.DensityGramsPerMl,
                GramPerPiece = p.GramPerPiece
            })
            .ToDictionaryAsync(p => p.Id, ct);

        decimal totalCalories = 0;

        foreach (EntryProjection entry in entries)
        {
            AccumulateRecipeCalories(entry, recipes, products, ref totalCalories);

            if (entry.Status == MealEntryStatus.Modified)
                AccumulateActualProductCalories(entry, products, ref totalCalories);
        }

        return new KcalResult((int)Math.Round(totalCalories, 0), mealsPlanned, mealsCompleted);
    }

    private static RecipeId? EffectiveRecipeId(EntryProjection entry)
        => entry.Status == MealEntryStatus.Modified ? entry.ActualRecipeId : entry.PlannedRecipeId;

    private static void AccumulateRecipeCalories(
        EntryProjection entry,
        IReadOnlyDictionary<RecipeId, RecipeProjection> recipes,
        IReadOnlyDictionary<ProductId, ProductProjection> products,
        ref decimal calories)
    {
        RecipeId? recipeId = EffectiveRecipeId(entry);
        if (recipeId is null || !recipes.TryGetValue(recipeId, out RecipeProjection? recipe)) return;

        decimal multiplier = recipe.Servings > 0 ? entry.Servings / recipe.Servings : 0;
        if (multiplier == 0) return;

        foreach (IngredientProjection ing in recipe.Ingredients)
        {
            if (!products.TryGetValue(ing.ProductId, out ProductProjection? product)) continue;

            decimal grams = UnitConverter.ConvertToGrams(
                ing.Amount, ing.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            decimal factor = grams / 100m * multiplier;

            calories += (product.Calories ?? 0) * factor;
        }
    }

    private static void AccumulateActualProductCalories(
        EntryProjection entry,
        IReadOnlyDictionary<ProductId, ProductProjection> products,
        ref decimal calories)
    {
        foreach (ActualProductProjection ap in entry.ActualProducts)
        {
            if (!products.TryGetValue(ap.ProductId, out ProductProjection? product)) continue;

            decimal grams = UnitConverter.ConvertToGrams(
                ap.Amount, ap.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            decimal factor = grams / 100m;

            calories += (product.Calories ?? 0) * factor;
        }
    }

    private sealed record KcalResult(int TotalKcal, int MealsPlanned, int MealsCompleted);

    private sealed class EntryProjection
    {
        public decimal Servings { get; init; }
        public MealEntryStatus Status { get; init; }
        public RecipeId PlannedRecipeId { get; init; } = default!;
        public RecipeId? ActualRecipeId { get; init; }
        public List<ActualProductProjection> ActualProducts { get; init; } = [];
    }

    private sealed class ActualProductProjection
    {
        public ProductId ProductId { get; init; } = default!;
        public decimal Amount { get; init; }
        public string Unit { get; init; } = default!;
    }

    private sealed class RecipeProjection
    {
        public RecipeId Id { get; init; } = default!;
        public int Servings { get; init; }
        public List<IngredientProjection> Ingredients { get; init; } = [];
    }

    private sealed class IngredientProjection
    {
        public ProductId ProductId { get; init; } = default!;
        public decimal Amount { get; init; }
        public string Unit { get; init; } = default!;
    }

    private sealed class ProductProjection
    {
        public ProductId Id { get; init; } = default!;
        public decimal? Calories { get; init; }
        public decimal? DensityGramsPerMl { get; init; }
        public decimal? GramPerPiece { get; init; }
    }
}
