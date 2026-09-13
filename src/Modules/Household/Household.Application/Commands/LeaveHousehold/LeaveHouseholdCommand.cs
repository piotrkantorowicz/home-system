namespace Household.Application.Commands.LeaveHousehold;

using Shared.Abstractions.Cqrs;

public sealed record LeaveHouseholdCommand(string RequestingAuthSubject, Guid HouseholdId) : ICommand;
