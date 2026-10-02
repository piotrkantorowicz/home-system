namespace Budget.Application.Queries.GetExpense;

using Shared.Abstractions.Cqrs;

/// <summary>Reads one expense in an envelope the caller may see; not-found when missing or invisible.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The expense.</param>
public sealed record GetExpenseQuery(string AuthSubject, Guid Id) : IQuery<ExpenseDto>;
