namespace Shared.Infrastructure.Messaging.Outbox;

using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Serialization;

public sealed class OutboxIntegrationEventBus : IIntegrationEventBus
{
    private readonly IOutboxStore _store;
    private readonly IIntegrationEventSerializer _serializer;

    public OutboxIntegrationEventBus(IOutboxStore store, IIntegrationEventSerializer serializer)
        => (_store, _serializer) = (store, serializer);

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message = new OutboxMessage(
            Id: Guid.NewGuid(),
            EventId: integrationEvent.EventId,
            EventType: integrationEvent.GetType().AssemblyQualifiedName!,
            Payload: _serializer.Serialize(integrationEvent),
            OccurredAt: integrationEvent.OccurredAt,
            ProcessedAt: null,
            AttemptCount: 0,
            LastError: null);

        return _store.AddAsync(message, ct);
    }
}
