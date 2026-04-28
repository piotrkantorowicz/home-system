namespace Notifications.Application.Templates;

using Notifications.Domain.ValueObjects;

public sealed record NotificationTemplate(
    NotificationType Type,
    string Locale,
    string TitleFormat,
    string BodyFormat);
