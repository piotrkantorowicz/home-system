namespace DietPlanner.Application.Queries.GetMealSchedule;

public sealed record MealSlotDto(Guid Id, string Name, string DefaultTime, int SortOrder);
public sealed record MealScheduleConfigDto(Guid Id, string UserId, IReadOnlyList<MealSlotDto> Slots, DateTime CreatedAt, DateTime? UpdatedAt);
