namespace Budget.Application.Commands.InitializeBudget;

using Budget.Application.Queries.GetBudget;

/// <summary>Outcome of <see cref="InitializeBudgetCommand"/>.</summary>
/// <param name="Budget">The household's budget.</param>
/// <param name="Created"><see langword="true"/> when this call created it, <see langword="false"/> when it already existed.</param>
public sealed record InitializeBudgetResult(BudgetDto Budget, bool Created);
