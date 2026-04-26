namespace Shared.Infrastructure.Messaging.Ef.Outbox;

public sealed class OutboxMessageEntity
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string EventType { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
