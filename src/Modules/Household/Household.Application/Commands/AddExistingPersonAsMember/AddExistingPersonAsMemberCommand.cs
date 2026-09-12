namespace Household.Application.Commands.AddExistingPersonAsMember;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

public sealed record AddExistingPersonAsMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role,
    string? Nickname) : ICommand;
