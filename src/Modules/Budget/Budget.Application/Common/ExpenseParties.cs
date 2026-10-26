namespace Budget.Application.Common;

using Budget.Domain.Aggregates;
using Budget.Domain.Exceptions;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Resolves who paid and who shares an expense; manual entry and statement import apply the same rules.</summary>
internal static class ExpenseParties
{
    /// <summary>Payer and participants for an expense in <paramref name="account"/>, checked against the current roster.</summary>
    /// <exception cref="BudgetDomainException">The funding, payer and participants do not fit the envelope.</exception>
    public static (PersonRef? PaidBy, List<PersonRef> Participants) Resolve(
        BudgetCaller caller, BudgetAccount account, FundingSource funding, Guid? paidByPersonId, IReadOnlyList<Guid>? participantIds)
        => account.Visibility == AccountVisibility.Personal
            ? ResolvePersonal(caller, account, funding, paidByPersonId, participantIds)
            : ResolveShared(caller, funding, paidByPersonId, participantIds);

    private static (PersonRef?, List<PersonRef>) ResolvePersonal(
        BudgetCaller caller, BudgetAccount account, FundingSource funding, Guid? paidByPersonId, IReadOnlyList<Guid>? participantIds)
    {
        if (funding != FundingSource.Individual)
            throw new BudgetDomainException("A personal envelope is funded individually.");
        if (paidByPersonId is { } p && p != account.OwnerPersonId)
            throw new BudgetDomainException("A personal expense is paid by the envelope's owner.");
        if (participantIds is { Count: > 0 })
            throw new BudgetDomainException("A personal expense is not shared.");

        var owner = caller.FindMember(account.OwnerPersonId!.Value)
            ?? throw new NotFoundException("Envelope", account.Id.Value);
        return (owner, []);
    }

    private static (PersonRef?, List<PersonRef>) ResolveShared(BudgetCaller caller, FundingSource funding, Guid? paidByPersonId, IReadOnlyList<Guid>? participantIds)
    {
        var ids = participantIds ?? [];
        if (funding == FundingSource.HouseholdFunds)
            return (paidByPersonId is null ? null : throw new BudgetDomainException("Household funds have no payer."),
                ids.Count == 0 ? [] : throw new BudgetDomainException("Household funds are not split."));

        var payer = paidByPersonId is { } payerId
            ? caller.FindAdult(payerId) ?? throw new BudgetDomainException("The payer must be a current owner or adult.")
            : throw new BudgetDomainException("An individually funded expense needs a payer.");

        if (ids.Count == 0)
            throw new BudgetDomainException("Choose at least one adult to share the cost.");

        var participants = ids
            .Select(id => caller.FindAdult(id) ?? throw new BudgetDomainException("Participants must be current owners or adults."))
            .ToList();
        return (payer, participants);
    }
}
