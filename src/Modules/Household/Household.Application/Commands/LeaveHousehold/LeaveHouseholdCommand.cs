namespace Household.Application.Commands.LeaveHousehold;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Removes the caller from the household. Any member may leave, except the last owner.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be a member.</param>
/// <param name="HouseholdId">The household acted on.</param>
public sealed record LeaveHouseholdCommand(string RequestingAuthSubject, Guid HouseholdId) : ICommand;
