namespace DietPlanner.Application.Commands.ResetMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Discards a meal entry's completion data and returns it to planned.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller.</param>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
public sealed record ResetMealEntryCommand(Guid Id, Guid PersonId) : ICommand;
