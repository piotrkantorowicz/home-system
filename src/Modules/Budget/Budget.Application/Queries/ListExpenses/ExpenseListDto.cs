namespace Budget.Application.Queries.ListExpenses;

using Budget.Application.Queries.GetExpense;

/// <summary>
/// One page of expenses plus exact aggregates over the whole filtered set, never just the loaded page.
/// Voided expenses can be listed (<c>includeVoided</c>) but never count towards any aggregate.
/// </summary>
/// <param name="Items">The expenses on this page, newest purchase first.</param>
/// <param name="TotalCount">Rows across all pages after filtering, voided ones included when requested.</param>
/// <param name="Page">The 1-based index of this page.</param>
/// <param name="PageSize">The effective page size.</param>
/// <param name="ActiveCount">Active (not voided) expenses across all pages.</param>
/// <param name="TotalAmount">Sum of active expenses across all pages, decimal string with two decimals.</param>
/// <param name="YourShareAmount">The caller's own stored shares of active split expenses; <c>"0.00"</c> when none.</param>
/// <param name="DailyTotals">Active totals per purchase date across all pages, newest first.</param>
public sealed record ExpenseListDto(
    IReadOnlyList<ExpenseDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int ActiveCount,
    string TotalAmount,
    string YourShareAmount,
    IReadOnlyList<ExpenseDayTotalDto> DailyTotals)
{
    /// <summary>The number of pages needed to show <see cref="TotalCount"/> rows at <see cref="PageSize"/> per page.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether a page after this one exists.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Whether a page before this one exists.</summary>
    public bool HasPreviousPage => Page > 1;
}
