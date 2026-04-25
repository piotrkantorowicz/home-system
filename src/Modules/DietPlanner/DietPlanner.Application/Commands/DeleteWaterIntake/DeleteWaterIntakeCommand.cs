namespace DietPlanner.Application.Commands.DeleteWaterIntake;

using Shared.Abstractions.Cqrs;

public sealed record DeleteWaterIntakeCommand(Guid Id, string UserId) : ICommand;
