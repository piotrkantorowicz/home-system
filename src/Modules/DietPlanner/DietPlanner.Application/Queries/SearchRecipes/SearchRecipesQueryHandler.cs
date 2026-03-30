namespace DietPlanner.Application.Queries.SearchRecipes;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
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

        var items = await q
            .OrderBy(r => r.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(Recipe.IngredientsField)
            .Select(r => new RecipeDto(
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
                    i.Amount,
                    i.Unit)).ToList()))
            .ToListAsync(ct);

        return new PagedList<RecipeDto>(items, totalCount, query.Page, query.PageSize);
    }
}
