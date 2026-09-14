namespace Shared.Infrastructure.Messaging.Outbox;

using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Serialization;

/// <summary>
/// The one <see cref="IIntegrationEventBus"/> implementation: serialises the event and stages it in
/// the publishing module's <see cref="IOutboxStore"/>. Nothing leaves the process here — the row is
/// committed by the module's unit of work and delivered later by <see cref="OutboxWorker{TDbContext}"/>.
/// </summary>
public sealed class OutboxIntegrationEventBus : IIntegrationEventBus
{
    private readonly IOutboxStore _store;
    private readonly IIntegrationEventSerializer _serializer;

    /// <summary>Creates the bus over the outbox store resolved for the current scope.</summary>
    /// <param name="store">The store routed to the publishing module (see <c>OutboxScope</c>).</param>
    /// <param name="serializer">Serialises events to the JSON payload column.</param>
    public OutboxIntegrationEventBus(IOutboxStore store, IIntegrationEventSerializer serializer)
        => (_store, _serializer) = (store, serializer);

    /// <inheritdoc />
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
