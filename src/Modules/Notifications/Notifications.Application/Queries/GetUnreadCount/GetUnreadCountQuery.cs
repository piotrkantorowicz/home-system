namespace Notifications.Application.Queries.GetUnreadCount;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Counts the caller's unread notifications, for the bell badge.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetUnreadCountQuery(string UserId) : IQuery<UnreadCountDto>;

/// <summary>
/// Unread notification count.
/// </summary>
/// <param name="Total">Number of notifications without a read time.</param>
public sealed record UnreadCountDto(int Total);
