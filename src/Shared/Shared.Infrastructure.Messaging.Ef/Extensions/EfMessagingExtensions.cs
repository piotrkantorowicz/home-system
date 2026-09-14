namespace Shared.Infrastructure.Messaging.Ef.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// DI registration for the EF Core-backed outbox and inbox used by Style-1 modules. The module's
/// <c>DbContext</c> must apply <see cref="OutboxMessageEntityConfiguration"/> and/or
/// <see cref="InboxMessageEntityConfiguration"/> so the tables are part of its migrations.
/// </summary>
public static class EfMessagingExtensions
{
    /// <summary>
    /// Registers the EF outbox store and a dedicated outbox worker for this DbContext.
    /// Call once per publishing module. Any number of publishing modules can coexist —
    /// each keeps its own keyed store, and the unkeyed <see cref="IOutboxStore"/> that
    /// <c>OutboxIntegrationEventBus</c> resolves routes to the right one via
    /// <see cref="OutboxScope.CurrentKey"/> (set by the domain-event dispatch boundary).
    /// </summary>
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<EfOutboxStore<TDbContext>>();

        // Keyed registration: this module's store, sharing its DbContext transaction.
        // OutboxWorker<TDbContext> and the routing resolver below both look it up by key.
        services.AddKeyedScoped<IOutboxStore>(typeof(TDbContext),
            (sp, _) => sp.GetRequiredService<EfOutboxStore<TDbContext>>());

        // Unkeyed registration used by OutboxIntegrationEventBus. Registered once (TryAdd) —
        // it routes to whichever module's keyed store OutboxScope names for the current
        // operation. Falls back to typeof(TDbContext) for direct publishes outside a
        // domain-event handler (single-module scenarios and tests).
        services.TryAddScoped<IOutboxStore>(sp =>
        {
            var key = OutboxScope.CurrentKey ?? typeof(TDbContext);
            return sp.GetRequiredKeyedService<IOutboxStore>(key);
        });

        services.AddHostedService<OutboxWorker<TDbContext>>();
        return services;
    }

    /// <summary>
    /// Registers an inbox-aware integration event handler bound to this consuming module's DbContext.
    /// Each consumer registration is independent; multiple modules can consume the same event type.
    /// </summary>
    public static IServiceCollection AddIntegrationEventConsumer<TEvent, THandler, TDbContext>(
        this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
        where TDbContext : DbContext
    {
        services.AddScoped<EfInboxExecutor<TDbContext>>();
        services.AddScoped<THandler>();

        // The decorator wraps THandler with the module's inbox executor, so the registered
        // IIntegrationEventHandler<TEvent> seen by the transport is already inbox-aware.
        services.AddScoped<IIntegrationEventHandler<TEvent>>(sp =>
            new InboxAwareHandler<TEvent>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<EfInboxExecutor<TDbContext>>()));

        return services;
    }
}
