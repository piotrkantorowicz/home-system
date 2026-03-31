namespace DietPlanner.Application.Queries.GetGoal;

public sealed record GoalDto(
    Guid Id,
    string UserId,
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
