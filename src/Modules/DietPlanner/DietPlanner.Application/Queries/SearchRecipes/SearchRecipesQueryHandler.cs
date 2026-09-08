namespace DietPlanner.Application.Queries.SearchRecipes;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Pagination;

internal sealed class SearchRecipesQueryHandler
    : IQueryHandler<SearchRecipesQuery, PagedList<RecipeDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public SearchRecipesQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<PagedList<RecipeDto>> HandleAsync(
        SearchRecipesQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Recipes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(r => r.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.OnlyMine)
            q = q.Where(r => r.CreatedByUserId == query.UserId);

        var totalCount = await q.CountAsync(ct);

        var recipes = await q
            .OrderBy(r => r.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(r => r.Ingredients)
            .ToListAsync(ct);

        // Batch-resolve products so list nutrition uses same conversion rules as detail.
        var allProductIds = recipes
            .SelectMany(r => r.Ingredients.Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var items = recipes.Select(recipe => ToDto(recipe, products, query.UserId)).ToList();

        return new PagedList<RecipeDto>(items, totalCount, query.Page, query.PageSize);
    }

    private static RecipeDto ToDto(Recipe recipe, IReadOnlyDictionary<ProductId, Product> products, string userId)
    {
        decimal calories = 0, protein = 0, carbs = 0, fat = 0, fiber = 0;
        foreach (var ingredient in recipe.Ingredients)
        {
            if (!products.TryGetValue(ingredient.ProductId, out var product)) continue;
            var factor = UnitConverter.ConvertToGrams(
                ingredient.Amount, ingredient.Unit, product.DensityGramsPerMl, product.GramPerPiece) / 100m;
            calories += (product.Nutrition.Calories ?? 0) * factor;
            protein += (product.Nutrition.Protein ?? 0) * factor;
            carbs += (product.Nutrition.Carbs ?? 0) * factor;
            fat += (product.Nutrition.Fat ?? 0) * factor;
            fiber += (product.Nutrition.Fiber ?? 0) * factor;
        }

        var servings = Math.Max(recipe.Servings, 1);
        return new RecipeDto(
            recipe.Id.Value, recipe.Name, recipe.Description, recipe.Instructions,
            recipe.Servings, recipe.PrepTimeMinutes, recipe.CreatedByUserId, recipe.CreatedAt,
            recipe.UpdatedAt, recipe.CreatedByUserId == userId,
            recipe.Ingredients.Select(i => new RecipeIngredientDto(
                i.Id.Value, i.ProductId.Value,
                products.GetValueOrDefault(i.ProductId)?.Name ?? "Unknown", i.Amount, i.Unit)).ToList(),
            new NutritionDto(calories / servings, protein / servings, carbs / servings, fat / servings, fiber / servings),
            new NutritionDto(calories, protein, carbs, fat, fiber));
    }
}
