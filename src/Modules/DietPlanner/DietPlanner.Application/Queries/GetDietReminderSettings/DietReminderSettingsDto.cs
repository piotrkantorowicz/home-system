namespace DietPlanner.Application.Queries.GetDietReminderSettings;

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
