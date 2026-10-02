namespace Budget.Application.Queries.GetBudget;

/// <summary>A household's budget as seen by an authorised caller.</summary>
/// <param name="Id">Budget identifier.</param>
/// <param name="Currency">ISO code: <c>PLN</c>, <c>EUR</c> or <c>USD</c>.</param>
public sealed record BudgetDto(Guid Id, string Currency);
