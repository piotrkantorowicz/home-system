namespace DietPlanner.Application.Commands.LogWaterIntake;

using Shared.Abstractions.Cqrs;

public sealed record LogWaterIntakeCommand(
    string UserId,
    DateOnly Date,
    int AmountMl,
    string? Note) : ICommand<Guid>;
