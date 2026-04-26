namespace Notifications.Api;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application;
using Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddNotificationsApplication()
            .AddNotificationsInfrastructure(configuration);

        return services;
    }

    public static void MigrateNotificationsDatabase(this IServiceProvider services)
        => services.MigrateNotifications();
}
