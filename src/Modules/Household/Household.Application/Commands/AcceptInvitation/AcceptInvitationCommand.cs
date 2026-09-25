namespace Household.Application.Commands.AcceptInvitation;

using Shared.Abstractions.Cqrs;

/// <summary>
/// The invitee accepts a pending invitation addressed to their own account email, joining the
/// household with the role and nickname it carries. Signing in alone never does this — it is
/// always an explicit action by the invited person.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be a <c>Person</c> whose email matches the invitation.</param>
/// <param name="InvitationId">The invitation to accept; must be pending and not expired.</param>
public sealed record AcceptInvitationCommand(
    string RequestingAuthSubject,
    Guid InvitationId) : ICommand;
