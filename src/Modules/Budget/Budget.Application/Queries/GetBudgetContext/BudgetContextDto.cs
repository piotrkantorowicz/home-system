namespace Budget.Application.Queries.GetBudgetContext;

using Budget.Application.Queries.GetAccount;
using Budget.Application.Queries.GetBudget;

/// <summary>The caller's budget setup, as <see cref="GetBudgetContextQuery"/> returns it.</summary>
/// <param name="Budget">The household's budget; <see langword="null"/> until it is initialised.</param>
/// <param name="Accounts">Active envelopes the caller may see, by name.</param>
/// <param name="Categories">Every category code an expense can carry.</param>
/// <param name="FundingSources">Every funding source code an expense can carry.</param>
/// <param name="Participants">Current owners and adults: who may pay for or share a household expense.</param>
public sealed record BudgetContextDto(
    BudgetDto? Budget,
    IReadOnlyList<AccountDto> Accounts,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> FundingSources,
    IReadOnlyList<BudgetParticipantDto> Participants);
