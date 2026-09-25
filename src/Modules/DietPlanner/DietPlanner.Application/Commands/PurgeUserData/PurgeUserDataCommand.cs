namespace DietPlanner.Application.Commands.PurgeUserData;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Deletes everything the Diet Planner stores for a user. Exposed only through the test-support endpoint so E2E runs can reset their worker users.
/// </summary>
/// <param name="UserId">Auth subject of the user to wipe.</param>
/// <param name="PersonId">Person identifier of the user to wipe.</param>
public sealed record PurgeUserDataCommand(string UserId, Guid PersonId) : ICommand;
