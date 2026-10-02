namespace Budget.Application.Commands.InitializeBudget;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Creates the household's budget and its default shared envelope, or returns the existing one.
/// Owner/Adult only.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Currency">Requested currency (<c>PLN</c>, <c>EUR</c>, <c>USD</c>); <see langword="null"/> means <c>PLN</c>. A different currency for an existing budget conflicts.</param>
public sealed record InitializeBudgetCommand(string AuthSubject, string? Currency)
    : ICommand<InitializeBudgetResult>;
