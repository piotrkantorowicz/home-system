namespace DietPlanner.Application.Queries.GetWeightPrediction;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Projects the caller's weight trajectory for a calorie target from their profile (BMR, TDEE, BMI, weekly change, estimated goal date). <see langword="null"/> when the profile lacks the fields the formulas need.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
/// <param name="DailyCalorieTarget">The hypothetical daily intake in kcal to project with.</param>
public sealed record GetWeightPredictionQuery(
    string UserId,
    decimal DailyCalorieTarget) : IQuery<WeightPredictionDto?>;
