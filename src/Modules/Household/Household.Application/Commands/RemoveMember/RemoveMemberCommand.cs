namespace Household.Application.Commands.RemoveMember;

using Shared.Abstractions.Cqrs;

public sealed record RemoveMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId) : ICommand;
