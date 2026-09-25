namespace DietPlanner.Application.Queries.GetWeeklySummary;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Computes the same weekly figures the weekly summary notification carries, for an arbitrary week, on demand.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="WeekStart">First day of the week.</param>
/// <param name="WeekEnd">Last day of the week.</param>
public sealed record GetWeeklySummaryQuery(
    Guid PersonId,
    DateOnly WeekStart,
    DateOnly WeekEnd) : IQuery<WeeklySummaryDto>;
