namespace Notifications.Infrastructure.Queries;

internal sealed record NotificationListRow(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string Payload,
    DateTime CreatedAt,
    DateTime? ReadAt);
