namespace Household.Application.Queries.ListMyInvitations;

using Shared.Abstractions.Cqrs;

/// <summary>Lists the pending invitations addressed to the caller's own account email.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
public sealed record ListMyInvitationsQuery(string AuthSubject) : IQuery<IReadOnlyList<MyInvitationDto>>;

/// <summary>A pending invitation addressed to the caller.</summary>
/// <param name="Id">Invitation identifier.</param>
/// <param name="HouseholdId">The household that would be joined on acceptance.</param>
/// <param name="HouseholdName">Display name of that household.</param>
/// <param name="Role">Role name granted on acceptance.</param>
/// <param name="CreatedAt">When it was issued, UTC.</param>
/// <param name="ExpiresAt">When it stops being acceptable, UTC.</param>
public sealed record MyInvitationDto(
    Guid Id,
    Guid HouseholdId,
    string HouseholdName,
    string Role,
    DateTime CreatedAt,
    DateTime ExpiresAt);
