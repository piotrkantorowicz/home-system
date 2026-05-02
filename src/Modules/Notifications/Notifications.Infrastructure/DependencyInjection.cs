namespace Notifications.Infrastructure;

using DietPlanner.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Notifications.Application.Channels;
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
using Notifications.Application.Templates;
using Notifications.Domain.Abstractions;
using Notifications.Infrastructure.Dispatching;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Migrations;
using Notifications.Infrastructure.Persistence.Repositories;
using Notifications.Infrastructure.Workers;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Cqrs.Extensions;
using Shared.Infrastructure.Messaging.Dapper;
using Shared.Infrastructure.Messaging.Dapper.Extensions;

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
        // IUnitOfWork is intentionally NOT registered globally to avoid colliding with
        // DietPlanner's binding. Application handlers commit through INotificationsUnitOfWork
        // (a Notifications-scoped abstraction) bound to DapperUnitOfWork here.
        services.AddScoped<INotificationsUnitOfWork>(sp => sp.GetRequiredService<DapperUnitOfWork>());

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationChannelPreferencesRepository, NotificationChannelPreferencesRepository>();
        services.AddScoped<IInboxStore, InboxStore>();

        services.AddCqrsHandlers(
            typeof(INotificationDispatcher).Assembly,            // Application
            typeof(NotificationDispatcher).Assembly);            // Infrastructure (query handlers)

        services.AddSingleton<INotificationTemplateRegistry, NotificationTemplateRegistry>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INotificationChannelSender, ConsoleNotificationChannelSender>());

        services.AddOptions<RetryDeliveryWorkerOptions>()
            .BindConfiguration(RetryDeliveryWorkerOptions.SectionName);
        services.AddHostedService<RetryDeliveryWorker>();

        // Idempotent inbox for incoming integration events (Dapper-backed; uses inbox_messages)
        services.AddDapperInbox<NotificationsConnectionFactory>();

        // Integration-event handler registrations (explicit per spec — no assembly scanning for these)
        services.AddScoped<
            IIntegrationEventHandler<MealReminderDueIntegrationEvent>,
            MealReminderDueIntegrationEventHandler>();
        services.AddScoped<
            IIntegrationEventHandler<MealMissedIntegrationEvent>,
            MealMissedIntegrationEventHandler>();
        services.AddScoped<
            IIntegrationEventHandler<GoalMilestoneReachedIntegrationEvent>,
            GoalMilestoneReachedIntegrationEventHandler>();
        services.AddScoped<
            IIntegrationEventHandler<WaterReminderDueIntegrationEvent>,
            WaterReminderDueIntegrationEventHandler>();
        services.AddScoped<
            IIntegrationEventHandler<WeeklySummaryDueIntegrationEvent>,
            WeeklySummaryDueIntegrationEventHandler>();

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
