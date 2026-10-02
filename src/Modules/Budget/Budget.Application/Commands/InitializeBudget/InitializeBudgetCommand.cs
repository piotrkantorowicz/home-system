namespace Budget.Application.Commands.InitializeBudget;

using Budget.Application.Queries.GetBudget;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Creates the household's budget and its default shared envelope, or returns the existing one.
/// Owner/Adult only.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Currency">Requested currency (<c>PLN</c>, <c>EUR</c>, <c>USD</c>); <see langword="null"/> means <c>PLN</c>. A different currency for an existing budget conflicts.</param>
public sealed record InitializeBudgetCommand(string AuthSubject, string? Currency)
    : ICommand<InitializeBudgetResult>;

/// <summary>Outcome of <see cref="InitializeBudgetCommand"/>.</summary>
/// <param name="Budget">The household's budget.</param>
/// <param name="Created"><see langword="true"/> when this call created it, <see langword="false"/> when it already existed.</param>
public sealed record InitializeBudgetResult(BudgetDto Budget, bool Created);
