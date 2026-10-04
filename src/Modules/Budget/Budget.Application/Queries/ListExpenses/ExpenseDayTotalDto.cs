namespace Budget.Application.Queries.ListExpenses;

/// <summary>Active spending on one purchase date across every page of the filtered list.</summary>
/// <param name="Date">Purchase date.</param>
/// <param name="Total">Sum of active expenses that day, decimal string with two decimals.</param>
/// <param name="Count">Number of active expenses that day.</param>
public sealed record ExpenseDayTotalDto(DateOnly Date, string Total, int Count);
