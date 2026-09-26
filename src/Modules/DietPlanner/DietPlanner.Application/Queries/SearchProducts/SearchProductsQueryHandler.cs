namespace DietPlanner.Application.Queries.SearchProducts;

using DietPlanner.Application.Households;
using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class SearchProductsQueryHandler
    : IQueryHandler<SearchProductsQuery, PagedList<ProductDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;
    private readonly HouseholdRosterProvider _households;

    public SearchProductsQueryHandler(IDietPlannerReadDbContext dbContext, HouseholdRosterProvider households)
    {
        _dbContext = dbContext;
        _households = households;
    }

    public async Task<PagedList<ProductDto>> HandleAsync(
        SearchProductsQuery query, CancellationToken ct = default)
    {
        LibraryAccess access = await _households.GetLibraryAccessAsync(query.UserId, ct);
        var q = access.Visible(_dbContext.Products.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLowerInvariant();
            // REASON: this lambda is an EF Core expression tree — ToLower()/Contains() translate to
            // SQL lower()/LIKE on the server; ToLowerInvariant and the StringComparison overload
            // have no translation and would throw at runtime.
#pragma warning disable CA1304, CA1311, CA1862
            q = q.Where(p => p.Name.ToLower().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

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
                p.CreatedByUserId == query.UserId,
                p.Visibility.ToString(),
                access.CanEdit(p.CreatedByUserId, p.Visibility)))
            .ToListAsync(ct);

        return new PagedList<ProductDto>(items, totalCount, query.Page, query.PageSize);
    }
}
