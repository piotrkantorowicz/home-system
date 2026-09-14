namespace Household.Application.Commands.CreateHousehold;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Creates a household with the caller as its first owner and returns its id. Fails if the caller already belongs to one.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller.</param>
/// <param name="Name">Display name; trimmed and capped at 120 characters.</param>
public sealed record CreateHouseholdCommand(string RequestingAuthSubject, string Name) : ICommand<Guid>;
