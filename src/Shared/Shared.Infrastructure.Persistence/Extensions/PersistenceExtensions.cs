namespace Shared.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers the <c>DomainEventDispatcherInterceptor</c> so it can be added to a
    /// module's <c>DbContext</c> via <c>opts.AddInterceptors(sp.GetServices&lt;ISaveChangesInterceptor&gt;())</c>.
    /// Domain events raised by aggregates are dispatched within the same SaveChanges flush.
    /// Idempotent — safe to call from every Style-1 module; the interceptor is registered once.
    /// </summary>
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        services.TryAddScoped<DomainEventDispatcherInterceptor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ISaveChangesInterceptor, DomainEventDispatcherInterceptor>());
        return services;
    }
}
