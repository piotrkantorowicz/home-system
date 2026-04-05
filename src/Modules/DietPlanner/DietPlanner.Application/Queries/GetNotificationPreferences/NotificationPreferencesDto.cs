namespace DietPlanner.Application.Queries.GetNotificationPreferences;

public sealed record NotificationPreferencesDto(
    Guid Id,
    string UserId,
    bool MealReminderEnabled,
    int MealReminderLeadTimeMinutes,
    bool WaterReminderEnabled,
    int WaterReminderIntervalMinutes,
    bool WeeklySummaryEnabled,
    bool GoalMilestoneAlertsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
