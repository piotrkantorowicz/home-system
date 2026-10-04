namespace Budget.Application.Queries.GetSummary;

/// <summary>Spending for one month and scope, with exact server totals.</summary>
/// <param name="Month"><c>YYYY-MM</c>; spending falls in the month of its purchase date.</param>
/// <param name="Scope"><c>Shared</c> (household envelopes) or <c>Personal</c> (one person's envelopes).</param>
/// <param name="Currency">The budget's currency.</param>
/// <param name="TotalSpent">Sum over every envelope in scope, decimal string with two decimals.</param>
/// <param name="ExpenseCount">Active expenses in the month across every envelope in scope; voided ones are excluded.</param>
/// <param name="Envelopes">One row per envelope in scope.</param>
/// <param name="Categories">Spending by category across the scope, largest first.</param>
public sealed record SummaryDto(
    string Month,
    string Scope,
    string Currency,
    string TotalSpent,
    int ExpenseCount,
    IReadOnlyList<SummaryEnvelopeDto> Envelopes,
    IReadOnlyList<SummaryCategoryDto> Categories);
