namespace Notifications.Infrastructure.Persistence.Repositories;

using Dapper;
using Notifications.Domain.Abstractions;
using Notifications.Domain.Models;
using Notifications.Infrastructure.Persistence.Sql;

internal sealed class NotificationChannelPreferencesRepository : INotificationChannelPreferencesRepository
{
    private readonly DapperUnitOfWork _uow;

    public NotificationChannelPreferencesRepository(DapperUnitOfWork uow) => _uow = uow;

    public async Task<NotificationChannelPreferences?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var connection = await _uow.GetConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<ChannelPreferencesRow>(new CommandDefinition(
            ChannelPreferencesSql.SelectByUserId,
            new { UserId = userId },
            cancellationToken: ct));

        return row is null ? null : NotificationMapping.ToDomain(row);
    }

    public async Task AddAsync(NotificationChannelPreferences preferences, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(new CommandDefinition(
            ChannelPreferencesSql.Insert,
            new
            {
                Id = preferences.Id.Value,
                preferences.UserId,
                preferences.ConsoleEnabled,
                preferences.EmailEnabled,
                preferences.WebSocketEnabled,
                preferences.UpdatedAt,
            },
            transaction: tx,
            cancellationToken: ct));
    }

    public async Task UpdateAsync(NotificationChannelPreferences preferences, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(new CommandDefinition(
            ChannelPreferencesSql.Update,
            new
            {
                Id = preferences.Id.Value,
                preferences.ConsoleEnabled,
                preferences.EmailEnabled,
                preferences.WebSocketEnabled,
                preferences.UpdatedAt,
            },
            transaction: tx,
            cancellationToken: ct));
    }
}
