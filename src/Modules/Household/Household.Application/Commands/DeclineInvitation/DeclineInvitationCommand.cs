namespace Household.Application.Commands.DeclineInvitation;

using Shared.Abstractions.Cqrs;

/// <summary>The invitee turns down a pending invitation addressed to their own account email.</summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be a <c>Person</c> whose email matches the invitation.</param>
/// <param name="InvitationId">The invitation to decline; must be pending.</param>
public sealed record DeclineInvitationCommand(
    string RequestingAuthSubject,
    Guid InvitationId) : ICommand;
