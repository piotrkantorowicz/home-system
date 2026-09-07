using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.RemoveMember;

public sealed record RemoveMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId) : ICommand;
