using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.AddExistingPersonAsMember;

public sealed record AddExistingPersonAsMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role,
    string? Nickname) : ICommand;
