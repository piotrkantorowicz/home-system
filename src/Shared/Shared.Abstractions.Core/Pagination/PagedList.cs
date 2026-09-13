namespace Shared.Abstractions.Core.Pagination;

/// <summary>
/// One page of a server-side paged query result plus the numbers a client needs to render paging
/// controls. Every list query returns this shape — never an unbounded list.
/// </summary>
/// <typeparam name="T">The DTO type of the items on the page.</typeparam>
/// <param name="Items">The items on this page, already ordered and sliced by the query.</param>
/// <param name="TotalCount">The number of items across all pages after filtering.</param>
/// <param name="Page">The 1-based index of this page.</param>
/// <param name="PageSize">The requested page size (clamped by the query validator, max 100).</param>
public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>The number of pages needed to show <see cref="TotalCount"/> items at <see cref="PageSize"/> per page.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether a page after this one exists.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Whether a page before this one exists.</summary>
    public bool HasPreviousPage => Page > 1;
}
