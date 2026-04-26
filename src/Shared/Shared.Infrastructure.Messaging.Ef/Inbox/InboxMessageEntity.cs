namespace Shared.Infrastructure.Messaging.Ef.Inbox;

public sealed class InboxMessageEntity
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = default!;
    public DateTime ConsumedAt { get; init; }
}
