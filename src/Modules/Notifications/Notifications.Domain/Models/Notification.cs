namespace Notifications.Domain.Models;

using Notifications.Domain.ValueObjects;

public sealed class Notification
{
    private Notification() { }

    public static Notification Create(
        NotificationId id,
        string userId,
        NotificationType type,
        string title,
        string body,
        string payload,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new Notification
        {
            Id = id,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Payload = payload,
            CreatedAt = createdAt,
        };
    }

    public NotificationId Id { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public void MarkRead(DateTime readAt)
    {
        if (ReadAt is not null) return;
        ReadAt = readAt;
    }
}
