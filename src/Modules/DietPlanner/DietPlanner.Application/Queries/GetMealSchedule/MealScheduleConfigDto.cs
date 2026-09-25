namespace DietPlanner.Application.Queries.GetMealSchedule;

/// <summary>
/// One slot of a user's meal schedule.
/// </summary>
/// <param name="Id">Identifier of the slot.</param>
/// <param name="Name">Display name.</param>
/// <param name="DefaultTime">Default time of day as <c>HH:mm</c>.</param>
/// <param name="SortOrder">Zero-based display position.</param>
public sealed record MealSlotDto(Guid Id, string Name, string DefaultTime, int SortOrder);
/// <summary>
/// A user's meal schedule with its slots in display order.
/// </summary>
/// <param name="Id">Identifier of the schedule.</param>
/// <param name="PersonId">Person identifier of the owner.</param>
/// <param name="Slots">The slots, ordered by <c>SortOrder</c>.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC; <see langword="null"/> if never changed.</param>
public sealed record MealScheduleConfigDto(Guid Id, Guid PersonId, IReadOnlyList<MealSlotDto> Slots, DateTime CreatedAt, DateTime? UpdatedAt);
