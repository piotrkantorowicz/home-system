namespace DietPlanner.Application.Commands.DeleteMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Removes a planned meal permanently.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record DeleteMealEntryCommand(Guid Id, string UserId) : ICommand;
