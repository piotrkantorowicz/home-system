namespace Investments.Application.Common;

using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Resolves the caller's current household and role through Household Contracts on every request.
/// Fails closed: no household is not-found, and a Guest or unknown role is denied. Never authorised
/// from cached rosters or JWT claims.
/// </summary>
internal sealed class InvestmentsAccessService(IHouseholdQueryService households)
{
    /// <summary>Resolves a caller allowed to use Investments at all: Owner, Adult or Child.</summary>
    /// <param name="authSubject">The caller's auth subject.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<InvestmentsCaller> RequireAccessAsync(string authSubject, CancellationToken ct)
    {
        var context = await households.GetHouseholdContextForUserAsync(authSubject, ct)
            ?? throw new NotFoundException("Household", authSubject);
        var caller = new InvestmentsCaller(context.HouseholdId, context.PersonId, context.Role, context.Members);

        return caller.IsAdult || caller.Role == "Child"
            ? caller
            : throw new ForbiddenException("Your household role has no access to investments.");
    }
}
