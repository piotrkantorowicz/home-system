namespace Budget.Application.Common;

using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Resolves the caller's current household and role through Household Contracts on every request.
/// Fails closed: a failed lookup propagates, no household is "setup required" rather than an empty
/// budget, and an unknown role is denied. Never authorised from cached rosters or JWT claims.
/// </summary>
internal sealed class BudgetAccessService(IHouseholdQueryService households)
{
    /// <summary>Resolves a caller allowed to use Budget at all: Owner, Adult or Child (a Guest has none).</summary>
    public async Task<BudgetCaller> RequireAccessAsync(string authSubject, CancellationToken ct)
    {
        var caller = await ResolveAsync(authSubject, ct);
        if (caller.IsAdult || caller.Role == "Child")
            return caller;

        throw new ForbiddenException("Your household role has no access to the budget.");
    }

    /// <summary>Resolves a caller who is an Owner or Adult.</summary>
    public async Task<BudgetCaller> RequireAdultAsync(string authSubject, CancellationToken ct)
    {
        var caller = await ResolveAsync(authSubject, ct);
        if (caller.IsAdult)
            return caller;

        throw new ForbiddenException("Only an owner or adult can do this.");
    }

    private async Task<BudgetCaller> ResolveAsync(string authSubject, CancellationToken ct)
    {
        var context = await households.GetHouseholdContextForUserAsync(authSubject, ct)
            ?? throw new NotFoundException("Household", authSubject);

        var managed = context.Members.Where(m => m.IsManaged).Select(m => m.PersonId).ToArray();
        return new BudgetCaller(context.HouseholdId, context.PersonId, context.Role, managed);
    }
}
