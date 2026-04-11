namespace DietPlanner.Application.Queries.GetWeightPrediction;

using Shared.Abstractions.CQRS;

public sealed record GetWeightPredictionQuery(
    string UserId,
    decimal DailyCalorieTarget) : IQuery<WeightPredictionDto?>;
