namespace DietPlanner.Application.Queries.SearchProducts;

using System.Linq.Expressions;
using DietPlanner.Application.Households;
using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
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
        var sort = (query.SortBy ?? "name").Trim().ToLowerInvariant();
        if (!SortKeys.Contains(sort))
            throw new CommandValidationException(
                nameof(SearchProductsQuery),
                [new ValidationError(nameof(query.SortBy), $"SortBy must be one of: {string.Join(", ", SortKeys)}.")]);

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

        if (query.OnlyIncomplete)
            q = q.Where(p => p.Nutrition.Calories == null || p.Nutrition.Protein == null
                || p.Nutrition.Carbs == null || p.Nutrition.Fat == null);

        var totalCount = await q.CountAsync(ct);

        var items = await Sorted(q, sort, query.SortDescending)
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

    private static readonly string[] SortKeys = ["name", "calories", "protein", "carbs", "fat", "fiber"];

    // Unknown (null) nutrition sorts last in both directions; name then id keep paging stable.
    private static IOrderedQueryable<Product> Sorted(IQueryable<Product> q, string sort, bool descending)
    {
        IOrderedQueryable<Product> By(Expression<Func<Product, bool>> isUnknown, Expression<Func<Product, decimal?>> value) => descending
            ? q.OrderBy(isUnknown).ThenByDescending(value)
            : q.OrderBy(isUnknown).ThenBy(value);

        var ordered = sort switch
        {
            "calories" => By(p => p.Nutrition.Calories == null, p => p.Nutrition.Calories),
            "protein" => By(p => p.Nutrition.Protein == null, p => p.Nutrition.Protein),
            "carbs" => By(p => p.Nutrition.Carbs == null, p => p.Nutrition.Carbs),
            "fat" => By(p => p.Nutrition.Fat == null, p => p.Nutrition.Fat),
            "fiber" => By(p => p.Nutrition.Fiber == null, p => p.Nutrition.Fiber),
            _ => descending ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
        };
        return sort == "name" ? ordered.ThenBy(p => p.Id) : ordered.ThenBy(p => p.Name).ThenBy(p => p.Id);
    }
}
