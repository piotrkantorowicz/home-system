namespace Notifications.Infrastructure.Persistence.Repositories;

using System.Reflection;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

internal sealed record NotificationRow(
    Guid Id,
    string UserId,
    string Type,
    string Title,
    string Body,
    string Payload,
    DateTime CreatedAt,
    DateTime? ReadAt);

internal sealed record NotificationDeliveryRow(
    Guid Id,
    Guid NotificationId,
    string Channel,
    string Status,
    int AttemptCount,
    DateTime? LastAttemptAt,
    DateTime? SentAt,
    string? FailureReason);

internal sealed record ChannelPreferencesRow(
    Guid Id,
    string UserId,
    bool ConsoleEnabled,
    bool EmailEnabled,
    bool WebSocketEnabled,
    DateTime UpdatedAt);

internal static class NotificationMapping
{
    internal static Notification ToDomain(NotificationRow row)
    {
        var notification = (Notification)Activator.CreateInstance(
            typeof(Notification),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null)!;

        SetPrivate(notification, "Id", NotificationId.From(row.Id));
        SetPrivate(notification, "UserId", row.UserId);
        SetPrivate(notification, "Type", Enum.Parse<NotificationType>(row.Type));
        SetPrivate(notification, "Title", row.Title);
        SetPrivate(notification, "Body", row.Body);
        SetPrivate(notification, "Payload", row.Payload);
        SetPrivate(notification, "CreatedAt", row.CreatedAt);
        SetPrivate(notification, "ReadAt", row.ReadAt);

        return notification;
    }

    internal static NotificationDelivery ToDomain(NotificationDeliveryRow row)
    {
        var delivery = (NotificationDelivery)Activator.CreateInstance(
            typeof(NotificationDelivery),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null)!;

        SetPrivate(delivery, "Id", NotificationDeliveryId.From(row.Id));
        SetPrivate(delivery, "NotificationId", NotificationId.From(row.NotificationId));
        SetPrivate(delivery, "Channel", Enum.Parse<NotificationChannel>(row.Channel));
        SetPrivate(delivery, "Status", Enum.Parse<DeliveryStatus>(row.Status));
        SetPrivate(delivery, "AttemptCount", row.AttemptCount);
        SetPrivate(delivery, "LastAttemptAt", row.LastAttemptAt);
        SetPrivate(delivery, "SentAt", row.SentAt);
        SetPrivate(delivery, "FailureReason", row.FailureReason);

        return delivery;
    }

    internal static NotificationChannelPreferences ToDomain(ChannelPreferencesRow row)
    {
        var prefs = (NotificationChannelPreferences)Activator.CreateInstance(
            typeof(NotificationChannelPreferences),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null)!;

        SetPrivate(prefs, "Id", NotificationChannelPreferencesId.From(row.Id));
        SetPrivate(prefs, "UserId", row.UserId);
        SetPrivate(prefs, "ConsoleEnabled", row.ConsoleEnabled);
        SetPrivate(prefs, "EmailEnabled", row.EmailEnabled);
        SetPrivate(prefs, "WebSocketEnabled", row.WebSocketEnabled);
        SetPrivate(prefs, "UpdatedAt", row.UpdatedAt);

        return prefs;
    }

    private static void SetPrivate<T>(T target, string propertyName, object? value)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Property {propertyName} not found on {typeof(T).Name}.");
        property.SetValue(target, value);
    }
}
