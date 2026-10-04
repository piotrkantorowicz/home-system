namespace Budget.Application.Queries.ListExpenses;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Pages through visible expenses, newest purchase first. Filters narrow only what the caller may
/// already see; the duplicate hint is this query with exact <paramref name="Amount"/>,
/// <paramref name="Category"/>, a ±2-day window and <paramref name="ExcludeId"/>.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page, capped at 100.</param>
/// <param name="AccountId">Only this envelope.</param>
/// <param name="Category">Only this category code.</param>
/// <param name="Amount">Only this exact amount (decimal string).</param>
/// <param name="From">Earliest purchase date, inclusive.</param>
/// <param name="To">Latest purchase date, inclusive.</param>
/// <param name="ExcludeId">Leave this expense out (the one being corrected).</param>
/// <param name="Search">Case-insensitive text matched against the description, after visibility and before paging.</param>
/// <param name="IncludeVoided">Also return voided expenses; by default only active ones, so they never count as duplicates.</param>
public sealed record ListExpensesQuery(
    string AuthSubject,
    int Page = 1,
    int PageSize = 20,
    Guid? AccountId = null,
    string? Category = null,
    string? Amount = null,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? ExcludeId = null,
    bool IncludeVoided = false,
    string? Search = null) : IQuery<ExpenseListDto>;
