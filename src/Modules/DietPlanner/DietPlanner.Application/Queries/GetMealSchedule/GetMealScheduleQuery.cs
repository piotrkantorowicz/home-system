namespace DietPlanner.Application.Queries.GetMealSchedule;

using Shared.Abstractions.CQRS;

public sealed record GetMealScheduleQuery(string UserId) : IQuery<MealScheduleConfigDto?>;
