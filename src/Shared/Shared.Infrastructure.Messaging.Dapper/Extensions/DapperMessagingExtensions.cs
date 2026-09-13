namespace Shared.Infrastructure.Messaging.Dapper.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Dapper.Inbox;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// DI registration for the Dapper-backed inbox used by Style-2 modules that consume integration
/// events. The inbox table (<c>inbox_messages</c>) is owned and migrated by the consuming module.
/// </summary>
public static class DapperMessagingExtensions
{
    /// <summary>
    /// Registers the Dapper-backed inbox executor for idempotent integration event handling
    /// with the given Npgsql connection factory.
    /// </summary>
    public static IServiceCollection AddDapperInbox<TFactory>(this IServiceCollection services)
        where TFactory : class, INpgsqlConnectionFactory
    {
        services.AddScoped<IInboxExecutor, DapperInboxExecutor<TFactory>>();
        return services;
    }

    /// <summary>
    /// Registers an inbox-aware integration event handler bound to this consuming module's
    /// Npgsql connection factory.
    /// </summary>
    public static IServiceCollection AddDapperIntegrationEventConsumer<TEvent, THandler, TFactory>(
        this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
        where TFactory : class, INpgsqlConnectionFactory
    {
        services.AddScoped<DapperInboxExecutor<TFactory>>();
        services.AddScoped<THandler>();

        services.AddScoped<IIntegrationEventHandler<TEvent>>(sp =>
            new InboxAwareHandler<TEvent>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<DapperInboxExecutor<TFactory>>()));

        return services;
    }
}
