namespace Notifications.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Domain.Abstractions;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Migrations;
using Notifications.Infrastructure.Persistence.Repositories;
using Npgsql;
using Shared.Abstractions.Core.Domain;
using Shared.Infrastructure.Messaging.Dapper;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Notifications")
            ?? throw new InvalidOperationException("Missing connection string 'Notifications'.");

        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

        services.AddScoped<NotificationsConnectionFactory>();
        services.AddScoped<INpgsqlConnectionFactory>(sp => sp.GetRequiredService<NotificationsConnectionFactory>());

        services.AddScoped<DapperUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DapperUnitOfWork>());

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
