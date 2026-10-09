namespace Notifications.Infrastructure.Queries;

using Dapper;
using Notifications.Application.Queries.ListNotifications;
using Notifications.Application.Templates;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Sql;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class ListNotificationsQueryHandler(DapperUnitOfWork uow, INotificationTemplateRegistry templates)
    : IQueryHandler<ListNotificationsQuery, PagedList<NotificationDto>>
{
    public async Task<PagedList<NotificationDto>> HandleAsync(
        ListNotificationsQuery query, CancellationToken ct = default)
    {
        var connection = await uow.GetConnectionAsync(ct);

        var items = await connection.QueryAsync<NotificationListRow>(new CommandDefinition(
            NotificationSql.ListByUserPaged,
            new
            {
                query.UserId,
                Offset = (query.Page - 1) * query.PageSize,
                Limit = query.PageSize,
            },
            cancellationToken: ct));

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            NotificationSql.CountByUser,
            new { query.UserId },
            cancellationToken: ct));

        var localizer = new NotificationTextLocalizer(templates);
        return new PagedList<NotificationDto>(items.Select(row => localizer.Localize(row, query.Locale)).ToList(),
            total, query.Page, query.PageSize);
    }
}
