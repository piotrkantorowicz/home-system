namespace DietPlanner.Application.Queries.GetWeeklySummary;

/// <summary>
/// A week's figures, identical to what the weekly summary notification carries.
/// </summary>
/// <param name="WeekStart">First day of the week.</param>
/// <param name="WeekEnd">Last day of the week.</param>
/// <param name="TotalKcal">Energy consumed over the week, kcal.</param>
/// <param name="TargetKcal">The daily calorie target multiplied by seven, kcal; 0 when no target is set.</param>
/// <param name="AvgWaterLiters">Average daily water intake, litres.</param>
/// <param name="WeightDeltaKg">Weight change from the first to the last weigh-in of the week, kilograms; <see langword="null"/> without two weigh-ins.</param>
/// <param name="MealsCompleted">Planned meals marked done or modified.</param>
/// <param name="MealsPlanned">Planned meals in the week.</param>
public sealed record WeeklySummaryDto(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    int TotalKcal,
    int TargetKcal,
    decimal AvgWaterLiters,
    decimal? WeightDeltaKg,
    int MealsCompleted,
    int MealsPlanned);
