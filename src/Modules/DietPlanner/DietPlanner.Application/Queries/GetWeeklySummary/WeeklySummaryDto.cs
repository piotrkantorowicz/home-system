namespace DietPlanner.Application.Queries.GetWeeklySummary;

public sealed record WeeklySummaryDto(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    int TotalKcal,
    int TargetKcal,
    decimal AvgWaterLiters,
    decimal? WeightDeltaKg,
    int MealsCompleted,
    int MealsPlanned);
