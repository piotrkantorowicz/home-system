namespace DietPlanner.Application.Queries.SearchProducts;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Pages through the products visible to the caller, optionally filtered by name, completeness and owner,
/// sorted by name or one nutrition column. Filters and sorting apply before paging, so the total always
/// describes the filtered set.
/// </summary>
/// <param name="Search">Case-insensitive substring to match against the name, or <see langword="null"/> for no filter.</param>
/// <param name="OnlyMine">When true, only items the caller created; otherwise everything visible to the caller.</param>
/// <param name="UserId">Auth subject of the caller.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page; clamped by the endpoint.</param>
/// <param name="OnlyIncomplete">When true, only products missing calories, protein, carbs or fat (fibre is optional and never makes a product incomplete).</param>
/// <param name="SortBy">One of <c>name</c> (default), <c>calories</c>, <c>protein</c>, <c>carbs</c>, <c>fat</c>, <c>fiber</c>; unknown nutrition values always sort last.</param>
/// <param name="SortDescending">Reverse the sort direction; ties always fall back to name then identifier, so paging is stable.</param>
public sealed record SearchProductsQuery(
    string? Search,
    bool OnlyMine,
    string UserId,
    int Page,
    int PageSize,
    bool OnlyIncomplete = false,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedList<ProductDto>>;
