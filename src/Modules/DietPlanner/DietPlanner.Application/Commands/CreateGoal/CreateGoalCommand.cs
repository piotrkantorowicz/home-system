namespace DietPlanner.Application.Commands.CreateGoal;

using Shared.Abstractions.Cqrs;

public sealed record CreateGoalCommand(
    string UserId,
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams) : ICommand<Guid>;
