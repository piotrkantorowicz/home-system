namespace Notifications.Infrastructure.Queries;

using System.Globalization;
using System.Text.Json;
using Notifications.Application.Queries.ListNotifications;
using Notifications.Application.Templates;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Dispatching;

internal sealed class NotificationTextLocalizer(INotificationTemplateRegistry templates)
{
    public NotificationDto Localize(NotificationListRow row, string locale)
    {
        if (locale != "pl" || !Enum.TryParse(row.Type, out NotificationType type))
            return Original(row);

        try
        {
            using var document = JsonDocument.Parse(row.Payload);
            var payload = document.RootElement;
            Dictionary<string, string> values = [];

            switch (type)
            {
                case NotificationType.MealReminder:
                case NotificationType.MealMissed:
                    if (!payload.TryGetProperty("MealSlotName", out var slot)
                        || !payload.TryGetProperty("PlannedAt", out var plannedAt)
                        || !DateTimeOffset.TryParse(plannedAt.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var time))
                        return Original(row);
                    values["MealSlotName"] = slot.GetString() ?? string.Empty;
                    values["PlannedAt"] = time.ToString("HH:mm", CultureInfo.InvariantCulture);
                    break;
                case NotificationType.WeeklySummary:
                    foreach (var name in new[] { "TotalKcal", "TargetKcal", "MealsCompleted", "MealsPlanned" })
                    {
                        if (!payload.TryGetProperty(name, out var value)) return Original(row);
                        values[name] = value.ToString();
                    }
                    break;
                case NotificationType.GoalMilestone:
                    if (!payload.TryGetProperty("Value", out var target)) return Original(row);
                    values["TargetWeightKg"] = target.ToString();
                    break;
                case NotificationType.WaterReminder:
                    break;
                default:
                    return Original(row);
            }

            var template = templates.Resolve(type, locale);
            return new NotificationDto(row.Id, row.Type,
                PlaceholderRenderer.Render(template.TitleFormat, values),
                PlaceholderRenderer.Render(template.BodyFormat, values), row.CreatedAt, row.ReadAt);
        }
        catch (JsonException)
        {
            return Original(row);
        }
        catch (InvalidOperationException)
        {
            return Original(row);
        }
    }

    private static NotificationDto Original(NotificationListRow row)
        => new(row.Id, row.Type, row.Title, row.Body, row.CreatedAt, row.ReadAt);
}
