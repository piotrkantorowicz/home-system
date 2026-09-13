namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Storage-agnostic view of one outbox row, exchanged between the bus, the <see cref="IOutboxStore"/>
/// and the transport. Persistence packages map it to their own entity.
/// </summary>
/// <param name="Id">Primary key of the outbox row (distinct from the event's own id).</param>
/// <param name="EventId">The published event's <c>EventId</c>, carried to consumers for idempotency.</param>
/// <param name="EventType">Assembly-qualified name of the event type, used to deserialise <paramref name="Payload"/>.</param>
/// <param name="Payload">The event serialised as JSON.</param>
/// <param name="OccurredAt">The event's <c>OccurredAt</c>; the worker dispatches in this order.</param>
/// <param name="ProcessedAt">When the transport delivered the message; <see langword="null"/> while pending.</param>
/// <param name="AttemptCount">How many dispatch attempts have failed so far.</param>
/// <param name="LastError">Message of the most recent dispatch failure, if any.</param>
public sealed record OutboxMessage(
    Guid Id,
    Guid EventId,
    string EventType,
    string Payload,
    DateTime OccurredAt,
    DateTime? ProcessedAt,
    int AttemptCount,
    string? LastError);
