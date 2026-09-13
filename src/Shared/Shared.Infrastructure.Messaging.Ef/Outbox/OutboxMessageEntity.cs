namespace Shared.Infrastructure.Messaging.Ef.Outbox;

/// <summary>
/// EF row in a publishing module's <c>outbox_messages</c> table — the persisted form of
/// <see cref="Shared.Infrastructure.Messaging.Outbox.OutboxMessage"/>. Inserted in the same
/// transaction as the aggregate change that published the event; the outbox worker later marks it
/// processed or records a failure.
/// </summary>
public sealed class OutboxMessageEntity
{
    /// <summary>Primary key of the outbox row (distinct from the event's own id).</summary>
    public Guid Id { get; init; }

    /// <summary>The published event's <c>EventId</c>, carried to consumers for idempotency.</summary>
    public Guid EventId { get; init; }

    /// <summary>Assembly-qualified name of the event type, used to deserialise <see cref="Payload"/>.</summary>
    public string EventType { get; init; } = default!;

    /// <summary>The event serialised as JSON (<c>jsonb</c> column).</summary>
    public string Payload { get; init; } = default!;

    /// <summary>The event's <c>OccurredAt</c>; the worker dispatches in this order.</summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>When the transport delivered the message; <see langword="null"/> while pending.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>How many dispatch attempts have failed so far.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Message of the most recent dispatch failure, if any.</summary>
    public string? LastError { get; set; }
}
