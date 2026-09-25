namespace DietPlanner.Application.Commands.DeleteWaterIntake;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Removes a logged drink permanently.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller.</param>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
public sealed record DeleteWaterIntakeCommand(Guid Id, Guid PersonId) : ICommand;
