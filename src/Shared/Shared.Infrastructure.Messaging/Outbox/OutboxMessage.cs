namespace Shared.Infrastructure.Messaging.Outbox;

public sealed record OutboxMessage(
    Guid Id,
    Guid EventId,
    string EventType,
    string Payload,
    DateTime OccurredAt,
    DateTime? ProcessedAt,
    int AttemptCount,
    string? LastError);
