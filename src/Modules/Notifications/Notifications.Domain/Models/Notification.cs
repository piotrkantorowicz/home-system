namespace Notifications.Domain.Models;

using Notifications.Domain.ValueObjects;

/// <summary>
/// One message for one user, already rendered from its template. Delivery per channel is tracked
/// separately in <see cref="NotificationDelivery"/>; the notification itself only tracks whether
/// the user has read it.
/// </summary>
public sealed class Notification
{
    private Notification() { }

    /// <summary>Creates an unread notification.</summary>
    /// <param name="id">Identifier for the new notification.</param>
    /// <param name="userId">Auth subject of the recipient; required.</param>
    /// <param name="type">What the notification is about.</param>
    /// <param name="title">Rendered title; required.</param>
    /// <param name="body">Rendered body; required.</param>
    /// <param name="payload">JSON with the source event's identifiers, for deep links; required.</param>
    /// <param name="createdAt">Creation time, UTC.</param>
    /// <exception cref="ArgumentException">Any string argument is blank.</exception>
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

    /// <summary>Identifier.</summary>
    public NotificationId Id { get; private set; } = default!;
    /// <summary>Auth subject of the recipient.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>What the notification is about.</summary>
    public NotificationType Type { get; private set; }
    /// <summary>Rendered title in the user's locale.</summary>
    public string Title { get; private set; } = default!;
    /// <summary>Rendered body in the user's locale.</summary>
    public string Body { get; private set; } = default!;
    /// <summary>JSON with the source event's identifiers (e.g. the meal entry), for deep links.</summary>
    public string Payload { get; private set; } = default!;
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>When the user read it, UTC; <see langword="null"/> while unread.</summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>Records that the user read the notification. Idempotent — the first read time is kept.</summary>
    /// <param name="readAt">The read time, UTC.</param>
    public void MarkRead(DateTime readAt)
    {
        if (ReadAt is not null) return;
        ReadAt = readAt;
    }
}
