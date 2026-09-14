namespace Notifications.Application;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Application-layer registration for the Notifications module; currently a placeholder kept for symmetry with the other modules.</summary>
public static class ApplicationDependencyInjection
{
    /// <summary>Registers application services (none yet).</summary>
    /// <param name="services">The host service collection.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        return services;
    }
}
