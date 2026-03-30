namespace DietPlanner.Application.Queries.GetNutritionSummary;

using Shared.Abstractions.CQRS;

public sealed record GetNutritionSummaryQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<DailyNutritionDto>>;
