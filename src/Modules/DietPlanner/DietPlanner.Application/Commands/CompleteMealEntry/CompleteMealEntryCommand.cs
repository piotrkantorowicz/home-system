namespace DietPlanner.Application.Commands.CompleteMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks a planned meal as eaten exactly as planned.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller or a managed member they look after.</param>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="AuthSubject">Auth subject of the caller; resolves their household.</param>
public sealed record CompleteMealEntryCommand(Guid Id, Guid PersonId, string AuthSubject) : ICommand;
