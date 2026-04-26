namespace Notifications.Infrastructure.Persistence.Repositories;

using System.Reflection;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

internal static class NotificationMapping
{
    internal static Notification ToDomain(NotificationRow row)
    {
        var notification = CreateInstance<Notification>();

        Set(notification, "Id", NotificationId.From(row.Id));
        Set(notification, "UserId", row.UserId);
        Set(notification, "Type", Enum.Parse<NotificationType>(row.Type));
        Set(notification, "Title", row.Title);
        Set(notification, "Body", row.Body);
        Set(notification, "Payload", row.Payload);
        Set(notification, "CreatedAt", row.CreatedAt);
        Set(notification, "ReadAt", row.ReadAt);

        return notification;
    }

    internal static NotificationDelivery ToDomain(NotificationDeliveryRow row)
    {
        var delivery = CreateInstance<NotificationDelivery>();

        Set(delivery, "Id", NotificationDeliveryId.From(row.Id));
        Set(delivery, "NotificationId", NotificationId.From(row.NotificationId));
        Set(delivery, "Channel", Enum.Parse<NotificationChannel>(row.Channel));
        Set(delivery, "Status", Enum.Parse<DeliveryStatus>(row.Status));
        Set(delivery, "AttemptCount", row.AttemptCount);
        Set(delivery, "LastAttemptAt", row.LastAttemptAt);
        Set(delivery, "SentAt", row.SentAt);
        Set(delivery, "FailureReason", row.FailureReason);

        return delivery;
    }

    internal static NotificationChannelPreferences ToDomain(ChannelPreferencesRow row)
    {
        var prefs = CreateInstance<NotificationChannelPreferences>();

        Set(prefs, "Id", NotificationChannelPreferencesId.From(row.Id));
        Set(prefs, "UserId", row.UserId);
        Set(prefs, "ConsoleEnabled", row.ConsoleEnabled);
        Set(prefs, "EmailEnabled", row.EmailEnabled);
        Set(prefs, "WebSocketEnabled", row.WebSocketEnabled);
        Set(prefs, "UpdatedAt", row.UpdatedAt);

        return prefs;
    }

    private static T CreateInstance<T>()
        => (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic, null, null, null)!;

    // FlattenHierarchy ensures inherited properties are found if the model ever
    // gains a base class — avoids a silent runtime failure from a plain typeof(T) lookup.
    private static void Set<T>(T target, string propertyName, object? value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        var property = typeof(T).GetProperty(propertyName, flags)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {typeof(T).Name}.");
        property.SetValue(target, value);
    }
}
