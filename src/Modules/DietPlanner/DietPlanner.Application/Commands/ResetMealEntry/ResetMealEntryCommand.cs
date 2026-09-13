namespace DietPlanner.Application.Commands.ResetMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Discards a meal entry's completion data and returns it to planned.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record ResetMealEntryCommand(Guid Id, string UserId) : ICommand;
