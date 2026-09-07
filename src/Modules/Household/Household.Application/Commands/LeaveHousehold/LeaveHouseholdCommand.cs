using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.LeaveHousehold;

public sealed record LeaveHouseholdCommand(string RequestingAuthSubject, Guid HouseholdId) : ICommand;
