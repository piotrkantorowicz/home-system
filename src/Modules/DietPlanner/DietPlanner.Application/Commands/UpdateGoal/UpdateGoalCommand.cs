namespace DietPlanner.Application.Commands.UpdateGoal;

using Shared.Abstractions.Cqrs;

public sealed record UpdateGoalCommand(
    string UserId,
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams) : ICommand;
