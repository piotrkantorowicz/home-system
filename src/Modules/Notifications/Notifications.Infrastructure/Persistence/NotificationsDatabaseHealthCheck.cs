namespace Notifications.Infrastructure.Persistence;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Healthy when the Notifications database answers a query; a thrown error reports the registration's failure status.</summary>
/// <param name="connections">The module's data source.</param>
internal sealed class NotificationsDatabaseHealthCheck(NotificationsConnectionFactory connections) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
