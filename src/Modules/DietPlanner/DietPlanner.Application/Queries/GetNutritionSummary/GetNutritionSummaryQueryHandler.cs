namespace DietPlanner.Application.Queries.GetNutritionSummary;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetNutritionSummaryQueryHandler
    : IQueryHandler<GetNutritionSummaryQuery, IReadOnlyList<DailyNutritionDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetNutritionSummaryQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<IReadOnlyList<DailyNutritionDto>> HandleAsync(
        GetNutritionSummaryQuery query, CancellationToken ct = default)
    {
        var entries = await _dbContext.MealEntries
            .AsNoTracking()
            .Where(me => me.PersonId == query.PersonId
                && (query.From == null || me.Date >= query.From)
                && (query.To == null || me.Date <= query.To))
            .Select(me => new EntryProjection
            {
                Date = me.Date,
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

        if (entries.Count == 0) return [];

        var recipeIds = entries
            .Select(EffectiveRecipeId)
            .Where(id => id is not null)
            .Select(id => id!)
            .Distinct()
            .ToList();

        var recipes = await _dbContext.Recipes
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

        var products = await _dbContext.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductProjection
            {
                Id = p.Id,
                Calories = p.Nutrition.Calories,
                Protein = p.Nutrition.Protein,
                Carbs = p.Nutrition.Carbs,
                Fat = p.Nutrition.Fat,
                Fiber = p.Nutrition.Fiber,
                DensityGramsPerMl = p.DensityGramsPerMl,
                GramPerPiece = p.GramPerPiece
            })
            .ToDictionaryAsync(p => p.Id, ct);

        return entries
            .GroupBy(e => e.Date)
            .Select(g =>
            {
                var total = NutritionInfo.Zero;

                foreach (var entry in g)
                {
                    total = total.Add(RecipeNutrition(entry, recipes, products));

                    if (entry.Status == MealEntryStatus.Modified)
                        total = total.Add(ActualProductsNutrition(entry, products));
                }

                return new DailyNutritionDto(g.Key,
                    Math.Round(total.Calories, 1),
                    Math.Round(total.Protein, 1),
                    Math.Round(total.Carbs, 1),
                    Math.Round(total.Fat, 1),
                    Math.Round(total.Fiber, 1));
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    private static RecipeId? EffectiveRecipeId(EntryProjection entry)
        => entry.Status == MealEntryStatus.Modified ? entry.ActualRecipeId : entry.PlannedRecipeId;

    private static NutritionInfo RecipeNutrition(
        EntryProjection entry,
        IReadOnlyDictionary<RecipeId, RecipeProjection> recipes,
        IReadOnlyDictionary<ProductId, ProductProjection> products)
    {
        var recipeId = EffectiveRecipeId(entry);
        if (recipeId is null || !recipes.TryGetValue(recipeId, out var recipe)) return NutritionInfo.Zero;

        var multiplier = recipe.Servings > 0 ? entry.Servings / recipe.Servings : 0;
        if (multiplier == 0) return NutritionInfo.Zero;

        var total = NutritionInfo.Zero;

        foreach (var ing in recipe.Ingredients)
        {
            if (!products.TryGetValue(ing.ProductId, out var product)) continue;

            var grams = UnitConverter.ConvertToGrams(
                ing.Amount, ing.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            var factor = grams / 100m * multiplier;

            total = total.Add(Scaled(product, factor));
        }

        return total;
    }

    private static NutritionInfo ActualProductsNutrition(
        EntryProjection entry,
        IReadOnlyDictionary<ProductId, ProductProjection> products)
    {
        var total = NutritionInfo.Zero;
        foreach (var ap in entry.ActualProducts)
        {
            if (!products.TryGetValue(ap.ProductId, out var product)) continue;

            var grams = UnitConverter.ConvertToGrams(
                ap.Amount, ap.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            var factor = grams / 100m;

            total = total.Add(Scaled(product, factor));
        }

        return total;
    }

    private static NutritionInfo Scaled(ProductProjection product, decimal factor) => new(
        (product.Calories ?? 0) * factor,
        (product.Protein ?? 0) * factor,
        (product.Carbs ?? 0) * factor,
        (product.Fat ?? 0) * factor,
        (product.Fiber ?? 0) * factor);

    private sealed class EntryProjection
    {
        public DateOnly Date { get; init; }
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
        public decimal? Protein { get; init; }
        public decimal? Carbs { get; init; }
        public decimal? Fat { get; init; }
        public decimal? Fiber { get; init; }
        public decimal? DensityGramsPerMl { get; init; }
        public decimal? GramPerPiece { get; init; }
    }
}
