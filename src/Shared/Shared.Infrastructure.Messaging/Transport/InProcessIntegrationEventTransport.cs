namespace Shared.Infrastructure.Messaging.Transport;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;

/// <summary>
/// v1 transport: deserialises the outbox message and invokes every registered
/// <see cref="IIntegrationEventHandler{TEvent}"/> for its type in a fresh DI scope, sequentially.
/// Handlers are already wrapped in the consuming module's inbox executor at registration time, so a
/// handler that throws leaves no inbox row and the message is retried on the next worker tick.
/// </summary>
public sealed class InProcessIntegrationEventTransport : IIntegrationEventTransport
{
    private readonly IServiceProvider _rootProvider;

    /// <summary>Creates the transport over the root provider used to open a scope per dispatch.</summary>
    /// <param name="rootProvider">The host's root service provider.</param>
    public InProcessIntegrationEventTransport(IServiceProvider rootProvider)
        => _rootProvider = rootProvider;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The message's event type cannot be resolved or deserialised.</exception>
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
