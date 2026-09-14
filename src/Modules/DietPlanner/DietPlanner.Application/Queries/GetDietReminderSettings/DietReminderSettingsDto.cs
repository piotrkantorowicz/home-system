namespace DietPlanner.Application.Queries.GetDietReminderSettings;

/// <summary>
/// A user's notification preferences as stored. All times are UTC.
/// </summary>
/// <param name="Id">Identifier of the settings row.</param>
/// <param name="UserId">Auth subject of the owner.</param>
/// <param name="MealRemindersEnabled">Whether meal notifications fire.</param>
/// <param name="MealReminderLeadTimeMinutes">Minutes before the planned time the reminder is sent.</param>
/// <param name="MealMissedGraceMinutes">Minutes after the planned time before an uncompleted meal counts as missed.</param>
/// <param name="WaterRemindersEnabled">Whether water reminders fire.</param>
/// <param name="WaterReminderIntervalMinutes">Minimum minutes between water reminders.</param>
/// <param name="WaterWindowStartUtc">Earliest UTC time of day a water reminder may fire.</param>
/// <param name="WaterWindowEndUtc">Latest UTC time of day a water reminder may fire.</param>
/// <param name="WeeklySummaryEnabled">Whether the weekly summary is sent.</param>
/// <param name="WeeklySummaryDayOfWeekUtc">UTC weekday the summary is sent on.</param>
/// <param name="WeeklySummaryTimeOfDayUtc">UTC time of day the summary is sent at.</param>
/// <param name="GoalAlertsEnabled">Whether goal milestone notifications fire.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC; <see langword="null"/> if never changed.</param>
public sealed record DietReminderSettingsDto(
    Guid Id,
    string UserId,
    bool MealRemindersEnabled,
    int MealReminderLeadTimeMinutes,
    int MealMissedGraceMinutes,
    bool WaterRemindersEnabled,
    int WaterReminderIntervalMinutes,
    TimeOnly WaterWindowStartUtc,
    TimeOnly WaterWindowEndUtc,
    bool WeeklySummaryEnabled,
    DayOfWeek WeeklySummaryDayOfWeekUtc,
    TimeOnly WeeklySummaryTimeOfDayUtc,
    bool GoalAlertsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
