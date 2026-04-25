namespace Shared.Infrastructure.Messaging.Outbox;

using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Serialization;

public sealed class OutboxIntegrationEventBus : IIntegrationEventBus
{
    private readonly IOutboxStore _store;
    private readonly IIntegrationEventSerializer _serializer;

    public OutboxIntegrationEventBus(IOutboxStore store, IIntegrationEventSerializer serializer)
        => (_store, _serializer) = (store, serializer);

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var message = new OutboxMessage(
            Id: Guid.NewGuid(),
            EventId: @event.EventId,
            EventType: typeof(TEvent).AssemblyQualifiedName!,
            Payload: _serializer.Serialize(@event),
            OccurredAt: @event.OccurredAt,
            ProcessedAt: null,
            AttemptCount: 0,
            LastError: null);

        return _store.AddAsync(message, ct);
    }
}
