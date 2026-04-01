namespace DietPlanner.Application.Queries.SearchRecipes;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Pagination;

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

        // Batch-resolve product names in a single query
        var allProductIds = recipes
            .SelectMany(r => r.Ingredients.Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var productNames = await _dbContext.Products
            .AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var items = recipes.Select(r => new RecipeDto(
            r.Id.Value,
            r.Name,
            r.Description,
            r.Instructions,
            r.Servings,
            r.PrepTimeMinutes,
            r.CreatedByUserId,
            r.CreatedAt,
            r.UpdatedAt,
            r.CreatedByUserId == query.UserId,
            r.Ingredients.Select(i => new RecipeIngredientDto(
                i.Id.Value,
                i.ProductId.Value,
                productNames.GetValueOrDefault(i.ProductId, "Unknown"),
                i.Amount,
                i.Unit)).ToList())).ToList();

        return new PagedList<RecipeDto>(items, totalCount, query.Page, query.PageSize);
    }
}
