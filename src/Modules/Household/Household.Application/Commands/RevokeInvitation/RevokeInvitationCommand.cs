using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.RevokeInvitation;

public sealed record RevokeInvitationCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid InvitationId) : ICommand;
