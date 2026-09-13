namespace DietPlanner.Application.Commands.DeleteWeightEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Removes a weigh-in permanently and re-syncs the profile's current weight to the latest remaining entry.
/// </summary>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
/// <param name="EntryId">Identifier of the entry; must belong to the caller.</param>
public sealed record DeleteWeightEntryCommand(string UserId, Guid EntryId) : ICommand;
