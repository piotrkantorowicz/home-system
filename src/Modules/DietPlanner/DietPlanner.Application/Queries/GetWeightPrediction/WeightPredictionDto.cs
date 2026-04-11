namespace DietPlanner.Application.Queries.GetWeightPrediction;

public sealed record WeightPredictionDto(
    decimal Bmr,
    decimal Tdee,
    decimal DailyDeficit,
    decimal WeeklyWeightChange,
    DateOnly? EstimatedGoalDate,
    decimal CurrentBmi,
    decimal? TargetBmi);
