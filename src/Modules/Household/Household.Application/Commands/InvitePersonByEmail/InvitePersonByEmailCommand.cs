namespace Household.Application.Commands.InvitePersonByEmail;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Invites someone by email: creates a 30-day pending invitation. Membership begins only once the
/// invitee explicitly accepts it — signing in with a matching email is not enough. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="Email">The invitee's address; normalised before matching.</param>
/// <param name="Role">The role granted; cannot be owner.</param>
public sealed record InvitePersonByEmailCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string Email,
    HouseholdRole Role) : ICommand<InvitePersonByEmailResult>;

/// <summary>Outcome of an invite: the pending invitation that was created.</summary>
/// <param name="InvitationId">The pending invitation's identifier.</param>
public sealed record InvitePersonByEmailResult(Guid InvitationId);
