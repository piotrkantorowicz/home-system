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

/// <summary>
/// The Notifications module's public registration surface: services, the SignalR hub configuration
/// and the WebSocket channel sender.
/// </summary>
public static class NotificationsModule
{
    /// <summary>Registers every Notifications service and SignalR. Call before <c>AddCqrsDispatchers()</c>.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the module's connection string and options.</param>
    /// <param name="environment">Enables detailed SignalR errors in Development.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="environment"/> is null.</exception>
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

    /// <summary>Runs the module's DbUp migrations; called by the host at startup in Development.</summary>
    /// <param name="services">The built host provider.</param>
    public static void MigrateNotificationsDatabase(this IServiceProvider services)
        => services.MigrateNotifications();
}
