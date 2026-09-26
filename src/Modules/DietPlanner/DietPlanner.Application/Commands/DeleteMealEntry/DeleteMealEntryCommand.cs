namespace DietPlanner.Application.Commands.DeleteMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Removes a planned meal permanently.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to someone the caller may plan for.</param>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="AuthSubject">Auth subject of the caller; resolves their household.</param>
public sealed record DeleteMealEntryCommand(Guid Id, Guid PersonId, string AuthSubject) : ICommand;
