namespace DietPlanner.Application.Queries.SearchProducts;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

public sealed record SearchProductsQuery(
    string? Search,
    bool OnlyMine,
    string UserId,
    int Page,
    int PageSize) : IQuery<PagedList<ProductDto>>;
