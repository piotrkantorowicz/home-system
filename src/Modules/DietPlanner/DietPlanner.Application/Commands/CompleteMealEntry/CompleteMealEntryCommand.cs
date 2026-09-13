namespace DietPlanner.Application.Commands.CompleteMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks a planned meal as eaten exactly as planned.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record CompleteMealEntryCommand(Guid Id, string UserId) : ICommand;
