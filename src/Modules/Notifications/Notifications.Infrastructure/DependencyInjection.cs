namespace Notifications.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Domain.Abstractions;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Migrations;
using Notifications.Infrastructure.Persistence.Repositories;
using Shared.Infrastructure.Messaging.Dapper;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Notifications")
            ?? throw new InvalidOperationException("Missing connection string 'Notifications'.");

        // NotificationsConnectionFactory owns NpgsqlDataSource internally (not in DI).
        // Registering NpgsqlDataSource as a bare DI singleton causes Npgsql EF Core
        // provider (v8+) to auto-adopt it for all DbContexts, corrupting other module tests.
        services.AddSingleton<NotificationsConnectionFactory>(
            _ => new NotificationsConnectionFactory(connectionString));
        services.AddSingleton<INpgsqlConnectionFactory>(
            sp => sp.GetRequiredService<NotificationsConnectionFactory>());

        services.AddScoped<DapperUnitOfWork>();
        // Note: IUnitOfWork is intentionally NOT registered globally here.
        // Notifications command handlers (N4) inject DapperUnitOfWork directly.
        // Two modules registering IUnitOfWork collide — the last one wins and
        // silently breaks the other module's command handlers.

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationChannelPreferencesRepository, NotificationChannelPreferencesRepository>();
        services.AddScoped<IInboxStore, InboxStore>();

        // Per-handler IInboxExecutor wiring lands in N4 (#154) via
        // AddDapperIntegrationEventConsumer<TEvent, THandler, NotificationsConnectionFactory>().
        return services;
    }

    public static void MigrateNotifications(this IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("Notifications")
            ?? throw new InvalidOperationException("Missing connection string 'Notifications'.");

        DbUpRunner.Run(connectionString);
    }
}
