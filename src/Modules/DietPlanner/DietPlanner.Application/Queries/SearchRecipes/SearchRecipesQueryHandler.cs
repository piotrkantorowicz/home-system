namespace DietPlanner.Application.Queries.SearchRecipes;

using DietPlanner.Application.Households;
using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class SearchRecipesQueryHandler
    : IQueryHandler<SearchRecipesQuery, PagedList<RecipeDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;
    private readonly HouseholdRosterProvider _households;

    public SearchRecipesQueryHandler(IDietPlannerReadDbContext dbContext, HouseholdRosterProvider households)
    {
        _dbContext = dbContext;
        _households = households;
    }

    public async Task<PagedList<RecipeDto>> HandleAsync(
        SearchRecipesQuery query, CancellationToken ct = default)
    {
        LibraryAccess access = await _households.GetLibraryAccessAsync(query.UserId, ct);
        var q = access.Visible(_dbContext.Recipes.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLowerInvariant();
            // REASON: this lambda is an EF Core expression tree — ToLower()/Contains() translate to
            // SQL lower()/LIKE on the server; ToLowerInvariant and the StringComparison overload
            // have no translation and would throw at runtime.
#pragma warning disable CA1304, CA1311, CA1862
            q = q.Where(r => r.Name.ToLower().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

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

        var items = recipes.Select(recipe => ToDto(recipe, products, access)).ToList();

        return new PagedList<RecipeDto>(items, totalCount, query.Page, query.PageSize);
    }

    private static RecipeDto ToDto(Recipe recipe, Dictionary<ProductId, Product> products, LibraryAccess access)
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
            recipe.UpdatedAt, recipe.CreatedByUserId == access.CallerSubject,
            recipe.Visibility.ToString(), access.CanEdit(recipe.CreatedByUserId, recipe.Visibility),
            recipe.Ingredients.Select(i => new RecipeIngredientDto(
                i.Id.Value, i.ProductId.Value,
                products.GetValueOrDefault(i.ProductId)?.Name ?? "Unknown", i.Amount, i.Unit)).ToList(),
            new NutritionDto(calories / servings, protein / servings, carbs / servings, fat / servings, fiber / servings),
            new NutritionDto(calories, protein, carbs, fat, fiber));
    }
}
