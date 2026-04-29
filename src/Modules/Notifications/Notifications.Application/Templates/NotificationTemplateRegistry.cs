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
                    "Time for {{MealSlotName}}",
                    "Your {{MealSlotName}} is planned at {{PlannedAt}}."),
            [(NotificationType.MealReminder, "pl")] =
                new(NotificationType.MealReminder, "pl",
                    "Czas na {{MealSlotName}}",
                    "Twój posiłek {{MealSlotName}} zaplanowano na {{PlannedAt}}."),

            [(NotificationType.MealMissed, "en")] =
                new(NotificationType.MealMissed, "en",
                    "Missed {{MealSlotName}}",
                    "You missed your {{MealSlotName}} planned for {{PlannedAt}}."),
            [(NotificationType.MealMissed, "pl")] =
                new(NotificationType.MealMissed, "pl",
                    "Pominięty {{MealSlotName}}",
                    "Pominąłeś posiłek {{MealSlotName}} zaplanowany na {{PlannedAt}}."),

            [(NotificationType.WaterReminder, "en")] =
                new(NotificationType.WaterReminder, "en",
                    "Water break",
                    "Time to drink some water."),
            [(NotificationType.WaterReminder, "pl")] =
                new(NotificationType.WaterReminder, "pl",
                    "Przerwa na wodę",
                    "Czas na wypicie wody."),

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
