namespace DietPlanner.Application.Queries.GetDietReminderSettings;

/// <summary>
/// A user's notification preferences as stored. Times of day are local; timestamps are UTC.
/// </summary>
/// <param name="Id">Identifier of the settings row.</param>
/// <param name="PersonId">Person identifier of the owner.</param>
/// <param name="MealRemindersEnabled">Whether meal notifications fire.</param>
/// <param name="MealReminderLeadTimeMinutes">Minutes before the planned time the reminder is sent.</param>
/// <param name="MealMissedGraceMinutes">Minutes after the planned time before an uncompleted meal counts as missed.</param>
/// <param name="WaterRemindersEnabled">Whether water reminders fire.</param>
/// <param name="WaterReminderIntervalMinutes">Minimum minutes between water reminders.</param>
/// <param name="WaterWindowStart">Earliest local time of day a water reminder may fire.</param>
/// <param name="WaterWindowEnd">Latest local time of day a water reminder may fire.</param>
/// <param name="WeeklySummaryEnabled">Whether the weekly summary is sent.</param>
/// <param name="WeeklySummaryDayOfWeek">Local weekday the summary is sent on.</param>
/// <param name="WeeklySummaryTimeOfDay">Local time of day the summary is sent at.</param>
/// <param name="GoalAlertsEnabled">Whether goal milestone notifications fire.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC; <see langword="null"/> if never changed.</param>
public sealed record DietReminderSettingsDto(
    Guid Id,
    Guid PersonId,
    bool MealRemindersEnabled,
    int MealReminderLeadTimeMinutes,
    int MealMissedGraceMinutes,
    bool WaterRemindersEnabled,
    int WaterReminderIntervalMinutes,
    TimeOnly WaterWindowStart,
    TimeOnly WaterWindowEnd,
    bool WeeklySummaryEnabled,
    DayOfWeek WeeklySummaryDayOfWeek,
    TimeOnly WeeklySummaryTimeOfDay,
    bool GoalAlertsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
