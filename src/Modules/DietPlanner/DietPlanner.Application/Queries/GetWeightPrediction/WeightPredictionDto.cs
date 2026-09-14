namespace DietPlanner.Application.Queries.GetWeightPrediction;

/// <summary>
/// Projection of a user's weight trajectory under a given daily calorie intake, derived from their profile.
/// </summary>
/// <param name="Bmr">Basal metabolic rate, kcal/day (Mifflin-St Jeor).</param>
/// <param name="Tdee">Total daily energy expenditure, kcal/day (BMR × activity multiplier).</param>
/// <param name="DailyDeficit">TDEE minus the requested intake, kcal/day; negative means a surplus.</param>
/// <param name="WeeklyWeightChange">Expected change per week, kilograms; negative means loss.</param>
/// <param name="EstimatedGoalDate">When the target weight would be reached at that rate. Today (UTC) when current and target weight already differ by less than 0.01 kg; <see langword="null"/> when no target is set, or the target is unreachable because the weekly change is zero or in the wrong direction.</param>
/// <param name="CurrentBmi">Body mass index at the current weight.</param>
/// <param name="TargetBmi">Body mass index at the target weight, if a target is set.</param>
public sealed record WeightPredictionDto(
    decimal Bmr,
    decimal Tdee,
    decimal DailyDeficit,
    decimal WeeklyWeightChange,
    DateOnly? EstimatedGoalDate,
    decimal CurrentBmi,
    decimal? TargetBmi);
