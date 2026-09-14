namespace Household.Application.Commands.DeleteHousehold;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Deletes the household and every membership; persons are kept. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
public sealed record DeleteHouseholdCommand(string RequestingAuthSubject, Guid HouseholdId) : ICommand;
