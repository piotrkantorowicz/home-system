namespace Notifications.Application.Queries.GetUnreadCount;

using Shared.Abstractions.Cqrs;

public sealed record GetUnreadCountQuery(string UserId) : IQuery<UnreadCountDto>;

public sealed record UnreadCountDto(int Total);
