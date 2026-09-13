namespace Household.Application.Commands.RenameHousehold;

using Shared.Abstractions.Cqrs;

public sealed record RenameHouseholdCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string Name) : ICommand;
