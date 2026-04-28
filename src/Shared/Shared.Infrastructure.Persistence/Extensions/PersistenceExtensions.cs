namespace Shared.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers the <c>DomainEventDispatcherInterceptor</c> so it can be added to a
    /// module's <c>DbContext</c> via <c>opts.AddInterceptors(sp.GetRequiredService&lt;ISaveChangesInterceptor&gt;())</c>.
    /// Domain events raised by aggregates are dispatched within the same SaveChanges flush.
    /// </summary>
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        services.AddScoped<DomainEventDispatcherInterceptor>();
        services.AddScoped<ISaveChangesInterceptor>(sp =>
            sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        return services;
    }
}
