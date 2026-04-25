namespace DietPlanner.Application.Queries.GetNutritionSummary;

using Shared.Abstractions.Cqrs;

public sealed record GetNutritionSummaryQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<DailyNutritionDto>>;
