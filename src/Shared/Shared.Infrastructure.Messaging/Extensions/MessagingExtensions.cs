namespace Shared.Infrastructure.Messaging.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Transport;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers the host-level messaging infrastructure: serializer + outbox bus + transport seam.
    /// Per-module outbox stores and consumers are registered via AddOutbox{TDbContext}() and
    /// AddIntegrationEventConsumer{TEvent, THandler, TDbContext}() in module DI.
    /// Outbox workers are registered as hosted services inside AddOutbox{TDbContext}().
    /// </summary>
    public static MessagingBuilder AddIntegrationEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationEventSerializer, IntegrationEventSerializer>();
        services.TryAddScoped<IIntegrationEventBus, OutboxIntegrationEventBus>();
        services.AddOptions<OutboxWorkerOptions>();

        return new MessagingBuilder(services);
    }

    public static MessagingBuilder UseInProcessTransport(this MessagingBuilder builder)
    {
        builder.Services.TryAddSingleton<IIntegrationEventTransport, InProcessIntegrationEventTransport>();
        return builder;
    }
}
