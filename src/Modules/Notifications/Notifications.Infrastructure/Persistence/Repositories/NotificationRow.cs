namespace Notifications.Infrastructure.Persistence.Repositories;

internal sealed record NotificationRow(
    Guid Id,
    string UserId,
    string Type,
    string Title,
    string Body,
    string Payload,
    DateTime CreatedAt,
    DateTime? ReadAt);
