namespace Notifications.Application.Queries.ListNotifications;

/// <summary>
/// A notification as shown in the inbox list.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Type">Notification type name.</param>
/// <param name="Title">Rendered title.</param>
/// <param name="Body">Rendered body.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="ReadAt">When the user read it, UTC; <see langword="null"/> while unread.</param>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    DateTime CreatedAt,
    DateTime? ReadAt);
