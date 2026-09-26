namespace DietPlanner.Application.Commands.UpdateDietReminderSettings;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Replaces the caller's notification preferences wholesale, creating them if they do not exist yet.
/// </summary>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="MealRemindersEnabled">Whether meal notifications fire.</param>
/// <param name="MealReminderLeadTimeMinutes">Minutes before the planned time to remind; positive.</param>
/// <param name="MealMissedGraceMinutes">Minutes after the planned time before a meal counts as missed; positive.</param>
/// <param name="WaterRemindersEnabled">Whether water reminders fire.</param>
/// <param name="WaterReminderIntervalMinutes">Minimum minutes between water reminders; positive.</param>
/// <param name="WaterWindowStart">Earliest local time of day a water reminder may fire.</param>
/// <param name="WaterWindowEnd">Latest local time of day a water reminder may fire; after the start.</param>
/// <param name="WeeklySummaryEnabled">Whether the weekly summary is sent.</param>
/// <param name="WeeklySummaryDayOfWeek">Local weekday the summary is sent on.</param>
/// <param name="WeeklySummaryTimeOfDay">Local time of day the summary is sent at.</param>
/// <param name="GoalAlertsEnabled">Whether goal milestone notifications fire.</param>
public sealed record UpdateDietReminderSettingsCommand(
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
    bool GoalAlertsEnabled) : ICommand;
