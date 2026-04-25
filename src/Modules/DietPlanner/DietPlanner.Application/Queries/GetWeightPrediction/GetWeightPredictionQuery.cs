namespace DietPlanner.Application.Queries.GetWeightPrediction;

using Shared.Abstractions.Cqrs;

public sealed record GetWeightPredictionQuery(
    string UserId,
    decimal DailyCalorieTarget) : IQuery<WeightPredictionDto?>;
