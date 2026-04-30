namespace DietPlanner.Application.Queries.GetWeeklySummary;

using Shared.Abstractions.Cqrs;

public sealed record GetWeeklySummaryQuery(
    string UserId,
    DateOnly WeekStart,
    DateOnly WeekEnd) : IQuery<WeeklySummaryDto>;
