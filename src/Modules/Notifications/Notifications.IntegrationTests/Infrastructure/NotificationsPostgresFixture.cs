namespace Notifications.IntegrationTests.Infrastructure;

using Notifications.Infrastructure.Persistence.Migrations;
using Testcontainers.PostgreSql;

/// <summary>
/// One PostgreSQL container for the Notifications integration suite with the module's DbUp scripts
/// applied, shared through <see cref="NotificationsDatabaseCollectionDefinition"/>.
/// </summary>
public sealed class NotificationsPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("notifications_test")
        .WithUsername("notifications")
        .WithPassword("notifications")
        .Build();

    /// <summary>Connection string of the running container.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Starts the container and runs the embedded DbUp migrations.</summary>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        DbUpRunner.Run(ConnectionString);
    }

    /// <summary>Stops and removes the container.</summary>
    public async ValueTask DisposeAsync()
        => await _container.DisposeAsync();
}

/// <summary>xUnit collection that shares one <see cref="NotificationsPostgresFixture"/> across the Notifications integration tests.</summary>
[CollectionDefinition(NotificationsDatabaseCollectionDefinition.Name)]
public sealed class NotificationsDatabaseCollectionDefinition : ICollectionFixture<NotificationsPostgresFixture>
{
    /// <summary>Collection name used in <c>[Collection]</c> attributes.</summary>
    public const string Name = "Notifications-Postgres";
}
