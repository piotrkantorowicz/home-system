using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.RenameHousehold;

public sealed record RenameHouseholdCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string Name) : ICommand;
