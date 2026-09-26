namespace Shared.Infrastructure.Messaging.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Host-level DI registration for the integration-event bus. Called once in <c>Program.cs</c>; modules
/// add their own outbox stores and consumers through the EF or Dapper extension packages.
/// </summary>
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
        services.AddOptions<OutboxWorkerOptions>().BindConfiguration(OutboxWorkerOptions.SectionName);

        return new MessagingBuilder(services);
    }

    /// <summary>
    /// Selects the v1 transport: outbox messages are dispatched to handlers in the same process,
    /// each inside a fresh DI scope and behind the consuming module's inbox executor. Replaced by a
    /// broker transport with a single different call here — module code is unaffected.
    /// </summary>
    public static MessagingBuilder UseInProcessTransport(this MessagingBuilder builder)
    {
        builder.Services.TryAddSingleton<IIntegrationEventTransport, InProcessIntegrationEventTransport>();
        return builder;
    }
}
