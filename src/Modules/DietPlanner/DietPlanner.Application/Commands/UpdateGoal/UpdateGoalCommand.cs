namespace DietPlanner.Application.Commands.UpdateGoal;

using Shared.Abstractions.CQRS;

public sealed record UpdateGoalCommand(
    string UserId,
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams) : ICommand;
