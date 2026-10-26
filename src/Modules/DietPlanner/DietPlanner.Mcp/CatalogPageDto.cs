namespace DietPlanner.Mcp;

/// <summary>
/// One page of catalog search results.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">Items on this page.</param>
/// <param name="TotalCount">Matches across all pages.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="HasNextPage">True when another page follows.</param>
public sealed record CatalogPageDto<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize, bool HasNextPage);
