namespace Budget.Application.Queries.GetBudget;

using Shared.Abstractions.Cqrs;

/// <summary>Reads the caller's household budget; <see langword="null"/> when it is not initialised yet.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
public sealed record GetBudgetQuery(string AuthSubject) : IQuery<BudgetDto?>;
