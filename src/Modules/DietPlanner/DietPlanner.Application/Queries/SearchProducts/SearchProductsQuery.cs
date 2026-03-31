namespace DietPlanner.Application.Queries.SearchProducts;

using Shared.Abstractions.CQRS;
using Shared.Abstractions.Pagination;

public sealed record SearchProductsQuery(
    string? Search,
    bool OnlyMine,
    string UserId,
    int Page,
    int PageSize) : IQuery<PagedList<ProductDto>>;
