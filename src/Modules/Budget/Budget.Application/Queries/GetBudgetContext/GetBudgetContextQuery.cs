namespace Budget.Application.Queries.GetBudgetContext;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Everything an agent needs before reading or recording expenses: the budget, the caller's active
/// envelopes, the category and funding codes, and who can pay for or share a household expense.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
public sealed record GetBudgetContextQuery(string AuthSubject) : IQuery<BudgetContextDto>;
