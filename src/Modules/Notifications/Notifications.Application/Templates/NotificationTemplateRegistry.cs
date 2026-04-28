namespace Notifications.Application.Templates;

using Notifications.Domain.ValueObjects;

internal sealed class NotificationTemplateRegistry : INotificationTemplateRegistry
{
    private const string FallbackLocale = "en";

    // v1 placeholder templates per spec; final wording lands with N7-N9.
    private static readonly IReadOnlyDictionary<(NotificationType, string), NotificationTemplate> Templates =
        new Dictionary<(NotificationType, string), NotificationTemplate>
        {
            [(NotificationType.MealReminder, "en")] =
                new(NotificationType.MealReminder, "en",
                    "Meal reminder",
                    "Your {{MealSlotName}} is coming up at {{PlannedAt}}."),
            [(NotificationType.MealMissed, "en")] =
                new(NotificationType.MealMissed, "en",
                    "Missed meal",
                    "You missed your {{MealSlotName}} planned for {{PlannedAt}}."),
            [(NotificationType.WaterReminder, "en")] =
                new(NotificationType.WaterReminder, "en",
                    "Water break",
                    "Time to drink some water."),
            [(NotificationType.WeeklySummary, "en")] =
                new(NotificationType.WeeklySummary, "en",
                    "Weekly nutrition summary",
                    "Last week: {{TotalKcal}} kcal of {{TargetKcal}} target, {{MealsCompleted}}/{{MealsPlanned}} meals."),
            [(NotificationType.GoalMilestone, "en")] =
                new(NotificationType.GoalMilestone, "en",
                    "Goal reached",
                    "{{MilestoneLabel}}"),
        };

    public NotificationTemplate Resolve(NotificationType type, string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);

        if (Templates.TryGetValue((type, locale), out var template))
            return template;

        if (Templates.TryGetValue((type, FallbackLocale), out var fallback))
            return fallback;

        throw new InvalidOperationException(
            $"No template registered for notification type '{type}' (locale '{locale}', fallback '{FallbackLocale}').");
    }
}
