namespace DietPlanner.Application.Workers;

internal sealed record WeeklyStats(
    int TotalKcal,
    int TargetKcal,
    decimal AvgWaterLiters,
    decimal? WeightDeltaKg,
    int MealsCompleted,
    int MealsPlanned);
