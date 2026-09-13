namespace DietPlanner.Application.Queries.SearchProducts;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class SearchProductsQueryHandler
    : IQueryHandler<SearchProductsQuery, PagedList<ProductDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public SearchProductsQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<PagedList<ProductDto>> HandleAsync(
        SearchProductsQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.OnlyMine)
            q = q.Where(p => p.CreatedByUserId == query.UserId);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Name,
                p.Nutrition.Calories,
                p.Nutrition.Protein,
                p.Nutrition.Carbs,
                p.Nutrition.Fat,
                p.Nutrition.Fiber,
                p.DefaultUnit,
                p.DensityGramsPerMl,
                p.GramPerPiece,
                p.CreatedByUserId,
                p.CreatedAt,
                p.UpdatedAt,
                p.CreatedByUserId == query.UserId))
            .ToListAsync(ct);

        return new PagedList<ProductDto>(items, totalCount, query.Page, query.PageSize);
    }
}
