namespace DietPlanner.Application.Commands.UpdateNotificationPreferences;

using Shared.Abstractions.CQRS;

public sealed record UpdateNotificationPreferencesCommand(
    string UserId,
    bool MealReminderEnabled,
    int MealReminderLeadTimeMinutes,
    bool WaterReminderEnabled,
    int WaterReminderIntervalMinutes,
    bool WeeklySummaryEnabled,
    bool GoalMilestoneAlertsEnabled) : ICommand;
