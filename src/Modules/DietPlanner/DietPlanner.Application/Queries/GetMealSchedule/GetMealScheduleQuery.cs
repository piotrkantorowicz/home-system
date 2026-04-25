namespace DietPlanner.Application.Queries.GetMealSchedule;

using Shared.Abstractions.Cqrs;

public sealed record GetMealScheduleQuery(string UserId) : IQuery<MealScheduleConfigDto?>;
