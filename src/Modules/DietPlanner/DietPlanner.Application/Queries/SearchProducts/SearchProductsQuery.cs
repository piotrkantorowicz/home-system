namespace DietPlanner.Application.Queries.SearchProducts;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Pages through the products visible to the caller, optionally filtered by name, ordered by name.
/// </summary>
/// <param name="Search">Case-insensitive substring to match against the name, or <see langword="null"/> for no filter.</param>
/// <param name="OnlyMine">When true, only items the caller created; otherwise everything visible to the caller.</param>
/// <param name="UserId">Auth subject of the caller.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page; clamped by the endpoint.</param>
public sealed record SearchProductsQuery(
    string? Search,
    bool OnlyMine,
    string UserId,
    int Page,
    int PageSize) : IQuery<PagedList<ProductDto>>;
