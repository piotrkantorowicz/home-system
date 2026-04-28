namespace Notifications.Application.Queries.ListNotifications;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

public sealed record ListNotificationsQuery(string UserId, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<NotificationDto>>;
