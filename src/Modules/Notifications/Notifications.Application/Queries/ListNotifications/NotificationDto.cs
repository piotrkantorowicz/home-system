namespace Notifications.Application.Queries.ListNotifications;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    DateTime CreatedAt,
    DateTime? ReadAt);
