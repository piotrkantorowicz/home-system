namespace DietPlanner.Application.Queries.GetNutritionSummary;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetNutritionSummaryQueryHandler
    : IQueryHandler<GetNutritionSummaryQuery, IReadOnlyList<DailyNutritionDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetNutritionSummaryQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<IReadOnlyList<DailyNutritionDto>> HandleAsync(
        GetNutritionSummaryQuery query, CancellationToken ct = default)
    {
        // Load meal entries with their recipe ingredients
        var mealData = await _dbContext.MealEntries
            .AsNoTracking()
            .Where(me => me.UserId == query.UserId
                && (query.From == null || me.Date >= query.From)
                && (query.To == null || me.Date <= query.To))
            .Join(_dbContext.Recipes.AsNoTracking().IgnoreQueryFilters().Include(r => r.Ingredients),
                me => me.RecipeId.Value,
                r => r.Id.Value,
                (me, r) => new
                {
                    me.Date,
                    MealServings = me.Servings,
                    RecipeServings = (decimal)r.Servings,
                    Ingredients = r.Ingredients
                })
            .ToListAsync(ct);

        if (mealData.Count == 0)
            return [];

        // Load product nutrition for all referenced products
        var productIds = mealData
            .SelectMany(x => x.Ingredients.Select(i => i.ProductId.Value))
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id.Value))
            .Select(p => new
            {
                Id = p.Id.Value,
                p.Nutrition.Calories,
                p.Nutrition.Protein,
                p.Nutrition.Carbs,
                p.Nutrition.Fat,
                p.Nutrition.Fiber
            })
            .ToDictionaryAsync(p => p.Id, ct);

        return mealData
            .GroupBy(x => x.Date)
            .Select(g =>
            {
                decimal calories = 0, protein = 0, carbs = 0, fat = 0, fiber = 0;

                foreach (var entry in g)
                {
                    var servingMultiplier = entry.RecipeServings > 0
                        ? entry.MealServings / entry.RecipeServings
                        : 0;

                    foreach (var ing in entry.Ingredients)
                    {
                        if (!products.TryGetValue(ing.ProductId.Value, out var product))
                            continue;

                        var amountFactor = ing.Amount / 100m * servingMultiplier;
                        calories += (product.Calories ?? 0) * amountFactor;
                        protein += (product.Protein ?? 0) * amountFactor;
                        carbs += (product.Carbs ?? 0) * amountFactor;
                        fat += (product.Fat ?? 0) * amountFactor;
                        fiber += (product.Fiber ?? 0) * amountFactor;
                    }
                }

                return new DailyNutritionDto(g.Key,
                    Math.Round(calories, 1),
                    Math.Round(protein, 1),
                    Math.Round(carbs, 1),
                    Math.Round(fat, 1),
                    Math.Round(fiber, 1));
            })
            .OrderBy(x => x.Date)
            .ToList();
    }
}
