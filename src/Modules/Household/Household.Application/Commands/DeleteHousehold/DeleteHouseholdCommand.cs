using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.DeleteHousehold;

public sealed record DeleteHouseholdCommand(string RequestingAuthSubject, Guid HouseholdId) : ICommand;
