namespace Household.Application.Commands.RevokeInvitation;

using Shared.Abstractions.Cqrs;

public sealed record RevokeInvitationCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid InvitationId) : ICommand;
