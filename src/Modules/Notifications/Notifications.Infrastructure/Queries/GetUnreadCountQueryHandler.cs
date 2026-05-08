namespace Notifications.Infrastructure.Queries;

using Dapper;

using Notifications.Application.Queries.GetUnreadCount;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Sql;
using Shared.Abstractions.Cqrs;

internal sealed class GetUnreadCountQueryHandler(DapperUnitOfWork uow)
    : IQueryHandler<GetUnreadCountQuery, UnreadCountDto>
{
    public async Task<UnreadCountDto> HandleAsync(
        GetUnreadCountQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.UserId);

        var connection = await uow.GetConnectionAsync(ct);
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            NotificationSql.CountUnreadByUser,
            new { query.UserId },
            cancellationToken: ct));

        return new UnreadCountDto(total);
    }
}
