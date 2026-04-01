namespace DietPlanner.Application.Queries.GetRecipeById;

using DietPlanner.Application.Persistence;
using DietPlanner.Application.Queries.SearchRecipes;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetRecipeByIdQueryHandler
    : IQueryHandler<GetRecipeByIdQuery, RecipeDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetRecipeByIdQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<RecipeDto?> HandleAsync(
        GetRecipeByIdQuery query, CancellationToken ct = default)
    {
        var recipe = await _dbContext.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .Where(r => r.Id == RecipeId.From(query.Id))
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
            return null;

        // Resolve product names and nutrition by joining with the Products table
        var productIds = recipe.Ingredients.Select(i => i.ProductId).ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var ingredientDtos = recipe.Ingredients.Select(i =>
        {
            var productName = products.TryGetValue(i.ProductId, out var product)
                ? product.Name
                : "Unknown";
            return new RecipeIngredientDto(i.Id.Value, i.ProductId.Value, productName, i.Amount, i.Unit);
        }).ToList();

        // Compute total nutrition from ingredients × product nutrition per 100g
        decimal totalCalories = 0, totalProtein = 0, totalCarbs = 0, totalFat = 0, totalFiber = 0;

        foreach (var ingredient in recipe.Ingredients)
        {
            if (!products.TryGetValue(ingredient.ProductId, out var product))
                continue;

            var factor = ingredient.Amount / 100m;
            totalCalories += (product.Nutrition.Calories ?? 0) * factor;
            totalProtein += (product.Nutrition.Protein ?? 0) * factor;
            totalCarbs += (product.Nutrition.Carbs ?? 0) * factor;
            totalFat += (product.Nutrition.Fat ?? 0) * factor;
            totalFiber += (product.Nutrition.Fiber ?? 0) * factor;
        }

        var totalNutrition = new NutritionDto(totalCalories, totalProtein, totalCarbs, totalFat, totalFiber);
        var servings = recipe.Servings > 0 ? recipe.Servings : 1;
        var perServing = new NutritionDto(
            totalCalories / servings,
            totalProtein / servings,
            totalCarbs / servings,
            totalFat / servings,
            totalFiber / servings);

        return new RecipeDto(
            recipe.Id.Value,
            recipe.Name,
            recipe.Description,
            recipe.Instructions,
            recipe.Servings,
            recipe.PrepTimeMinutes,
            recipe.CreatedByUserId,
            recipe.CreatedAt,
            recipe.UpdatedAt,
            recipe.CreatedByUserId == query.UserId,
            ingredientDtos,
            perServing,
            totalNutrition);
    }
}
