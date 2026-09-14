namespace Household.Application.Queries.ListPendingInvitations;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Lists the household's invitations that are still pending.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller; must be a member.</param>
/// <param name="HouseholdId">The household acted on.</param>
public sealed record ListPendingInvitationsQuery(string AuthSubject, Guid HouseholdId)
    : IQuery<IReadOnlyList<InvitationDto>>;

/// <summary>
/// A pending invitation.
/// </summary>
/// <param name="Id">Invitation identifier.</param>
/// <param name="Email">The invited address.</param>
/// <param name="Role">Role name granted on acceptance.</param>
/// <param name="Status">Status name; always <c>Pending</c> from this query.</param>
/// <param name="CreatedAt">When it was issued, UTC.</param>
/// <param name="ExpiresAt">When it stops being acceptable, UTC.</param>
public sealed record InvitationDto(
    Guid Id,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt);
