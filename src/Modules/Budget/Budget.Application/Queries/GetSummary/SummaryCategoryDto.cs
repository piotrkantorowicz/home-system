namespace Budget.Application.Queries.GetSummary;

/// <summary>Spending in one category for the scope and month.</summary>
/// <param name="Category">Category code.</param>
/// <param name="Spent">Decimal string with two decimals.</param>
public sealed record SummaryCategoryDto(string Category, string Spent);
