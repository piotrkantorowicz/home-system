namespace Household.Application.Commands.RenameHousehold;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Renames the household. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="Name">New display name; trimmed and capped at 120 characters.</param>
public sealed record RenameHouseholdCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string Name) : ICommand;
