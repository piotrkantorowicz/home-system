namespace Shared.Infrastructure.Messaging.Outbox;

using System.Diagnostics;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Telemetry;

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

        // Producer span: child of the caller's span (e.g. the API request). Its id is what the consumer
        // span later parents on, so request -> event -> handler share one TraceId. Null when nothing listens.
        using var producer = MessagingTracing.Source.StartActivity(
            $"{integrationEvent.GetType().Name} publish", ActivityKind.Producer);
        producer?.SetTag(MessagingTracing.EventTypeTag, integrationEvent.GetType().Name);
        producer?.SetTag(MessagingTracing.EventIdTag, integrationEvent.EventId);

        var message = new OutboxMessage(
            Id: Guid.CreateVersion7(),
            EventId: integrationEvent.EventId,
            EventType: integrationEvent.GetType().AssemblyQualifiedName!,
            Payload: _serializer.Serialize(integrationEvent),
            OccurredAt: integrationEvent.OccurredAt,
            ProcessedAt: null,
            AttemptCount: 0,
            LastError: null,
            // Only a W3C id fits the varchar(55) column and parses back as a traceparent.
            TraceParent: producer?.IdFormat == ActivityIdFormat.W3C ? producer.Id : null);

        return _store.AddAsync(message, ct);
    }
}
