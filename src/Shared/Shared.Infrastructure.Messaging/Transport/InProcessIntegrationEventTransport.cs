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

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = sp.GetServices(handlerType).ToList();

        if (handlers.Count == 0) return;

        var inbox = sp.GetRequiredService<IInboxExecutor>();

        foreach (var handler in handlers)
        {
            await inbox.ExecuteAsync(
                eventId: message.EventId,
                eventType: message.EventType,
                handlerInvocation: async invocationCt =>
                {
                    var task = (Task)handlerType
                        .GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!
                        .Invoke(handler, [@event, invocationCt])!;
                    await task.ConfigureAwait(false);
                },
                ct: ct);
        }
    }
}
