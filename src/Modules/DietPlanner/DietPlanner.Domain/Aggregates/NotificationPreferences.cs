namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class NotificationPreferences : AggregateRoot<NotificationPreferencesId>
{
    private NotificationPreferences() { }

    public static NotificationPreferences Create(
        NotificationPreferencesId id,
        string userId,
        bool mealReminderEnabled = true,
        int mealReminderLeadTimeMinutes = 15,
        bool waterReminderEnabled = true,
        int waterReminderIntervalMinutes = 60,
        bool weeklySummaryEnabled = true,
        bool goalMilestoneAlertsEnabled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new NotificationPreferences
        {
            Id = id,
            UserId = userId,
            MealReminderEnabled = mealReminderEnabled,
            MealReminderLeadTimeMinutes = mealReminderLeadTimeMinutes,
            WaterReminderEnabled = waterReminderEnabled,
            WaterReminderIntervalMinutes = waterReminderIntervalMinutes,
            WeeklySummaryEnabled = weeklySummaryEnabled,
            GoalMilestoneAlertsEnabled = goalMilestoneAlertsEnabled,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string UserId { get; private set; } = default!;
    public bool MealReminderEnabled { get; private set; }
    public int MealReminderLeadTimeMinutes { get; private set; }
    public bool WaterReminderEnabled { get; private set; }
    public int WaterReminderIntervalMinutes { get; private set; }
    public bool WeeklySummaryEnabled { get; private set; }
    public bool GoalMilestoneAlertsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void Update(
        bool mealReminderEnabled,
        int mealReminderLeadTimeMinutes,
        bool waterReminderEnabled,
        int waterReminderIntervalMinutes,
        bool weeklySummaryEnabled,
        bool goalMilestoneAlertsEnabled)
    {
        MealReminderEnabled = mealReminderEnabled;
        MealReminderLeadTimeMinutes = mealReminderLeadTimeMinutes;
        WaterReminderEnabled = waterReminderEnabled;
        WaterReminderIntervalMinutes = waterReminderIntervalMinutes;
        WeeklySummaryEnabled = weeklySummaryEnabled;
        GoalMilestoneAlertsEnabled = goalMilestoneAlertsEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
