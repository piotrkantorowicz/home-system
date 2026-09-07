using Household.Domain.Abstractions;
using Household.Domain.Aggregates;

namespace Household.Application.Common;

/// <summary>
/// Resolves a pending email invitation for a person the moment they become reachable —
/// i.e. on login (their <c>Person</c> now exists) or when a managed person is linked to an
/// account. Adds them to the household and marks the invitation accepted. Does not commit —
/// the caller owns the unit of work.
/// </summary>
internal sealed class InvitationResolver
{
    private readonly IHouseholdInvitationRepository _invitations;
    private readonly IHouseholdRepository _households;

    public InvitationResolver(IHouseholdInvitationRepository invitations, IHouseholdRepository households)
        => (_invitations, _households) = (invitations, households);

    public async Task TryResolveForAsync(Person person, CancellationToken ct)
    {
        if (person.Email is null)
            return;

        if (await _households.GetByMemberPersonIdAsync(person.Id, ct) is not null)
            return;

        var invitation = await _invitations.GetPendingByEmailAsync(person.Email, ct);
        if (invitation is null)
            return;

        var now = DateTime.UtcNow;

        if (invitation.HasExpired(now))
        {
            invitation.Expire(now);
            return;
        }

        var household = await _households.GetByIdAsync(invitation.HouseholdId, ct);
        if (household is null)
        {
            invitation.Expire(now);
            return;
        }

        household.AddMember(person.Id, invitation.Role);
        invitation.Accept(now);
    }
}
