namespace Shared.Infrastructure.Messaging.Ef.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;

public static class EfMessagingExtensions
{
    /// <summary>
    /// Registers the EF outbox store and a dedicated outbox worker for this DbContext.
    /// Call once per publishing module.
    /// </summary>
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<EfOutboxStore<TDbContext>>();

        // Keyed registration: OutboxWorker<TDbContext> resolves this to get its module's store.
        services.AddKeyedScoped<IOutboxStore>(typeof(TDbContext),
            (sp, _) => sp.GetRequiredService<EfOutboxStore<TDbContext>>());

        // Unkeyed registration: IIntegrationEventBus (OutboxIntegrationEventBus) resolves IOutboxStore.
        // For v1 single-publisher this is fine; a future multi-publisher scenario would route
        // via a per-bus keyed resolution instead.
        services.AddScoped<IOutboxStore>(sp => sp.GetRequiredService<EfOutboxStore<TDbContext>>());

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
