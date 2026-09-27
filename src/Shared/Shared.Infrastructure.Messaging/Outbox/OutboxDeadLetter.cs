namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>One dead-lettered outbox row as shown to an admin; the payload is left out.</summary>
/// <param name="Id">Primary key of the outbox row; pass it to requeue.</param>
/// <param name="EventId">The event's own id.</param>
/// <param name="EventType">Assembly-qualified name of the event type.</param>
/// <param name="OccurredAt">When the event was raised, UTC.</param>
/// <param name="AttemptCount">Failed dispatch attempts so far.</param>
/// <param name="LastError">Message of the most recent failure.</param>
public sealed record OutboxDeadLetter(
    Guid Id,
    Guid EventId,
    string EventType,
    DateTime OccurredAt,
    int AttemptCount,
    string? LastError);
