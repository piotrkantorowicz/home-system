using System.ComponentModel;

namespace DietPlanner.Api.Common.Models;

/// <summary>
/// Generic paginated response wrapper.
/// </summary>
public class PagedResult<T>
{
    [Description("List of items for the current page")]
    public List<T> Items { get; set; } = new();

    [Description("Total number of items across all pages")]
    public int TotalCount { get; set; }

    [Description("Current page number (1-based)")]
    public int Page { get; set; }

    [Description("Number of items per page")]
    public int PageSize { get; set; }

    [Description("Total number of pages")]
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [Description("Whether a previous page exists")]
    public bool HasPreviousPage => Page > 1;

    [Description("Whether a next page exists")]
    public bool HasNextPage => Page < TotalPages;
}
