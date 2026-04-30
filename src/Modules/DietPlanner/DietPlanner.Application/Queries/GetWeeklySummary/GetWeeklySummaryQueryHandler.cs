namespace DietPlanner.Application.Queries.GetWeeklySummary;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetWeeklySummaryQueryHandler
    : IQueryHandler<GetWeeklySummaryQuery, WeeklySummaryDto>
{
    private const decimal MlPerLiter = 1000m;
    private const int DaysInWeek = 7;

    private readonly IDietPlannerReadDbContext _db;

    public GetWeeklySummaryQueryHandler(IDietPlannerReadDbContext db) => _db = db;

    public async Task<WeeklySummaryDto> HandleAsync(
        GetWeeklySummaryQuery query, CancellationToken ct = default)
    {
        int targetKcal = await ComputeTargetKcalAsync(query.UserId, ct);
        KcalResult kcalResult = await ComputeTotalKcalAndCountsAsync(query, ct);
        decimal avgWaterLiters = await ComputeAvgWaterLitersAsync(query, ct);
        decimal? weightDeltaKg = await ComputeWeightDeltaAsync(query, ct);

        return new WeeklySummaryDto(
            WeekStart: query.WeekStart,
            WeekEnd: query.WeekEnd,
            TotalKcal: kcalResult.TotalKcal,
            TargetKcal: targetKcal,
            AvgWaterLiters: avgWaterLiters,
            WeightDeltaKg: weightDeltaKg,
            MealsCompleted: kcalResult.MealsCompleted,
            MealsPlanned: kcalResult.MealsPlanned);
    }

    private async Task<int> ComputeTargetKcalAsync(string userId, CancellationToken ct)
    {
        var goal = await _db.UserGoals
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .Select(g => g.DailyCalorieTarget)
            .FirstOrDefaultAsync(ct);

        return (goal ?? 0) * 7;
    }

    private async Task<decimal> ComputeAvgWaterLitersAsync(
        GetWeeklySummaryQuery query, CancellationToken ct)
    {
        int totalMl = await _db.WaterIntakes
            .AsNoTracking()
            .Where(w => w.UserId == query.UserId
                && w.Date >= query.WeekStart
                && w.Date <= query.WeekEnd)
            .SumAsync(w => w.AmountMl, ct);

        return Math.Round(totalMl / MlPerLiter / DaysInWeek, 2);
    }

    private async Task<decimal?> ComputeWeightDeltaAsync(
        GetWeeklySummaryQuery query, CancellationToken ct)
    {
        var weights = await _db.WeightEntries
            .AsNoTracking()
            .Where(w => w.UserId == query.UserId
                && w.Date >= query.WeekStart
                && w.Date <= query.WeekEnd)
            .OrderBy(w => w.Date)
            .Select(w => w.WeightKg)
            .ToListAsync(ct);

        if (weights.Count < 2) return null;

        return Math.Round(weights[weights.Count - 1] - weights[0], 2);
    }

    private async Task<KcalResult> ComputeTotalKcalAndCountsAsync(
        GetWeeklySummaryQuery query, CancellationToken ct)
    {
        var entries = await _db.MealEntries
            .AsNoTracking()
            .Where(me => me.UserId == query.UserId
                && me.Date >= query.WeekStart
                && me.Date <= query.WeekEnd)
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

        var recipeIds = entries
            .Select(EffectiveRecipeId)
            .Where(id => id is not null)
            .Select(id => id!)
            .Distinct()
            .ToList();

        var recipes = await _db.Recipes
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

        var productIds = recipes.Values
            .SelectMany(r => r.Ingredients.Select(i => i.ProductId))
            .Concat(entries.SelectMany(e => e.ActualProducts.Select(ap => ap.ProductId)))
            .Distinct()
            .ToList();

        var products = await _db.Products
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

        foreach (var entry in entries)
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
        var recipeId = EffectiveRecipeId(entry);
        if (recipeId is null || !recipes.TryGetValue(recipeId, out var recipe)) return;

        var multiplier = recipe.Servings > 0 ? entry.Servings / recipe.Servings : 0;
        if (multiplier == 0) return;

        foreach (var ing in recipe.Ingredients)
        {
            if (!products.TryGetValue(ing.ProductId, out var product)) continue;

            var grams = UnitConverter.ConvertToGrams(
                ing.Amount, ing.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            var factor = grams / 100m * multiplier;

            calories += (product.Calories ?? 0) * factor;
        }
    }

    private static void AccumulateActualProductCalories(
        EntryProjection entry,
        IReadOnlyDictionary<ProductId, ProductProjection> products,
        ref decimal calories)
    {
        foreach (var ap in entry.ActualProducts)
        {
            if (!products.TryGetValue(ap.ProductId, out var product)) continue;

            var grams = UnitConverter.ConvertToGrams(
                ap.Amount, ap.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            var factor = grams / 100m;

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
