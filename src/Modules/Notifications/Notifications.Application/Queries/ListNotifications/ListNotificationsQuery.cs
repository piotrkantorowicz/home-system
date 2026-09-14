namespace Notifications.Application.Queries.ListNotifications;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Pages through the caller's notifications, newest first.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
public sealed record ListNotificationsQuery(string UserId, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<NotificationDto>>;
