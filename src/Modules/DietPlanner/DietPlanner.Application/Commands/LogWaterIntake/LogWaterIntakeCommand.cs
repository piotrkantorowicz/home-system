namespace DietPlanner.Application.Commands.LogWaterIntake;

using Shared.Abstractions.CQRS;

public sealed record LogWaterIntakeCommand(
    string UserId,
    DateOnly Date,
    int AmountMl,
    string? Note) : ICommand<Guid>;
