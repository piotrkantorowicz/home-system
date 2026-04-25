namespace Shared.Infrastructure.Messaging.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Transport;

public static class MessagingExtensions
{
    public static MessagingBuilder AddIntegrationEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationEventSerializer, IntegrationEventSerializer>();
        services.TryAddScoped<IIntegrationEventBus, OutboxIntegrationEventBus>();
        services.AddOptions<OutboxWorkerOptions>();
        services.AddHostedService<OutboxWorker>();

        return new MessagingBuilder(services);
    }

    public static MessagingBuilder UseInProcessTransport(this MessagingBuilder builder)
    {
        builder.Services.TryAddSingleton<IIntegrationEventTransport, InProcessIntegrationEventTransport>();
        return builder;
    }
}
