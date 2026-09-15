namespace Household.Application.Common;

using Household.Domain.Abstractions;
using Household.Domain.Aggregates;

/// <summary>
/// Resolves a pending email invitation for a person the moment they become reachable —
/// i.e. on login (their <c>Person</c> now exists) or when a managed person is linked to an
/// account. Adds them to the household and marks the invitation accepted. Does not commit —
/// the caller owns the unit of work.
/// </summary>
internal sealed class InvitationResolver(
    IHouseholdInvitationRepository invitations,
    IHouseholdRepository households,
    TimeProvider clock)
{
    public async Task TryResolveForAsync(Person person, CancellationToken ct)
    {
        if (person.Email is null)
            return;

        if (await households.GetByMemberPersonIdAsync(person.Id, ct) is not null)
            return;

        var invitation = await invitations.GetPendingByEmailAsync(person.Email, ct);
        if (invitation is null)
            return;

        var now = clock.GetUtcNow().UtcDateTime;

        if (invitation.HasExpired(now))
        {
            invitation.Expire(now);
            return;
        }

        var household = await households.GetByIdAsync(invitation.HouseholdId, ct);
        if (household is null)
        {
            invitation.Expire(now);
            return;
        }

        household.AddMember(person.Id, invitation.Role, now);
        invitation.Accept(now);
    }
}
