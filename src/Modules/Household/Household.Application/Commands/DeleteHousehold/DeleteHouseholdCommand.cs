namespace Household.Application.Commands.DeleteHousehold;

using Shared.Abstractions.Cqrs;

public sealed record DeleteHouseholdCommand(string RequestingAuthSubject, Guid HouseholdId) : ICommand;
