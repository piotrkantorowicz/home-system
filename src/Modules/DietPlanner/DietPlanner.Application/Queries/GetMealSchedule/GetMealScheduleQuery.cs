namespace DietPlanner.Application.Queries.GetMealSchedule;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's meal schedule with its slots; <see langword="null"/> until one has been created.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetMealScheduleQuery(string UserId) : IQuery<MealScheduleConfigDto?>;
