namespace Notifications.Api;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Notifications.Api.Channels;
using Notifications.Api.SignalR;
using Notifications.Application;
using Notifications.Application.Channels;
using Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        services
            .AddNotificationsApplication()
            .AddNotificationsInfrastructure(configuration);

        services.AddSingleton<INotificationConnectionRegistry, NotificationConnectionRegistry>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INotificationChannelSender, WebSocketNotificationChannelSender>());

        services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            options.EnableDetailedErrors = environment.IsDevelopment();
        });

        return services;
    }

    public static void MigrateNotificationsDatabase(this IServiceProvider services)
        => services.MigrateNotifications();
}
