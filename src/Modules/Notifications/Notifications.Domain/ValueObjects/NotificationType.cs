namespace Notifications.Domain.ValueObjects;

/// <summary>
/// What a notification is about. Selects the template it is rendered with; each value corresponds
/// to one Diet Planner integration event. Stored as text.
/// </summary>
public enum NotificationType
{
    /// <summary>A planned meal is coming up.</summary>
    MealReminder,
    /// <summary>A planned meal was not completed within the grace period.</summary>
    MealMissed,
    /// <summary>Time to drink water.</summary>
    WaterReminder,
    /// <summary>Last week's nutrition figures.</summary>
    WeeklySummary,
    /// <summary>A goal (target weight) was reached.</summary>
    GoalMilestone,
}
