namespace Household.Application.Commands.RevokeInvitation;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Withdraws a pending invitation. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="InvitationId">The invitation to revoke; must belong to the household and be pending.</param>
public sealed record RevokeInvitationCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid InvitationId) : ICommand;
