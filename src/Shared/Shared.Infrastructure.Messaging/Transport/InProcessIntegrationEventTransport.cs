namespace Shared.Infrastructure.Messaging.Transport;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;

public sealed class InProcessIntegrationEventTransport : IIntegrationEventTransport
{
    private readonly IServiceProvider _rootProvider;

    public InProcessIntegrationEventTransport(IServiceProvider rootProvider)
        => _rootProvider = rootProvider;

    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        var eventType = Type.GetType(message.EventType)
            ?? throw new InvalidOperationException($"Unknown event type: {message.EventType}");

        await using var scope = _rootProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var serializer = sp.GetRequiredService<IIntegrationEventSerializer>();
        var @event = serializer.Deserialize(message.Payload, message.EventType);

        // Handlers are pre-decorated with InboxAwareHandler<T> at registration time via
        // AddIntegrationEventConsumer<TEvent, THandler, TDbContext>(), so the transport
        // just invokes them — no transport-level inbox concern.
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = sp.GetServices(handlerType).ToList();

        foreach (var handler in handlers)
        {
            var task = (Task)handlerType
                .GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!
                .Invoke(handler, [@event, ct])!;
            await task.ConfigureAwait(false);
        }
    }
}
