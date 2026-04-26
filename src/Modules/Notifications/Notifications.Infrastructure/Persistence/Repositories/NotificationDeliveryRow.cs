namespace Notifications.Infrastructure.Persistence.Repositories;

internal sealed record NotificationDeliveryRow(
    Guid Id,
    Guid NotificationId,
    string Channel,
    string Status,
    int AttemptCount,
    DateTime? LastAttemptAt,
    DateTime? SentAt,
    string? FailureReason);
