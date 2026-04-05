namespace DietPlanner.Application.Commands.DeleteWaterIntake;

using Shared.Abstractions.CQRS;

public sealed record DeleteWaterIntakeCommand(Guid Id, string UserId) : ICommand;
