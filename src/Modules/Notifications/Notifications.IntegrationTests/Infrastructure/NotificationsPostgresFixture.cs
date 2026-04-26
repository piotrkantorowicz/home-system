namespace Notifications.IntegrationTests.Infrastructure;

using Notifications.Infrastructure.Persistence.Migrations;
using Testcontainers.PostgreSql;

public sealed class NotificationsPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("notifications_test")
        .WithUsername("notifications")
        .WithPassword("notifications")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DbUpRunner.Run(ConnectionString);
    }

    public async Task DisposeAsync()
        => await _container.DisposeAsync();
}

[CollectionDefinition(NotificationsDatabaseCollection.Name)]
public sealed class NotificationsDatabaseCollection : ICollectionFixture<NotificationsPostgresFixture>
{
    public const string Name = "Notifications-Postgres";
}
