namespace DietPlanner.Application.Queries.GetNutritionSummary;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Aggregates the caller's meal entries into per-day macro totals over a date range, using actual products for modified meals.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
/// <param name="From">First day to include, or <see langword="null"/> for no lower bound.</param>
/// <param name="To">Last day to include, or <see langword="null"/> for no upper bound.</param>
public sealed record GetNutritionSummaryQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<DailyNutritionDto>>;
