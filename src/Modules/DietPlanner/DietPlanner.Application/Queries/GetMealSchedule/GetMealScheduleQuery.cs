namespace DietPlanner.Application.Queries.GetMealSchedule;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's meal schedule with its slots; <see langword="null"/> until one has been created.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
public sealed record GetMealScheduleQuery(Guid PersonId) : IQuery<MealScheduleConfigDto?>;
