namespace DietPlanner.Application.Commands.ResetMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Discards a meal entry's completion data and returns it to planned.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller or a managed member they look after.</param>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="AuthSubject">Auth subject of the caller; resolves their household.</param>
public sealed record ResetMealEntryCommand(Guid Id, Guid PersonId, string AuthSubject) : ICommand;
