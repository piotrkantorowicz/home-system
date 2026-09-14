namespace Shared.Infrastructure.Messaging.Ef.Inbox;

/// <summary>
/// EF row in a consuming module's <c>inbox_messages</c> table. One row per integration event the
/// module has consumed; its presence is what makes redelivery a no-op. Written only by
/// <see cref="EfInboxExecutor{TDbContext}"/>, inside the handler's transaction.
/// </summary>
public sealed class InboxMessageEntity
{
    /// <summary>The consumed event's <c>EventId</c>; primary key and idempotency key.</summary>
    public Guid EventId { get; init; }

    /// <summary>Assembly-qualified name of the event type, kept for diagnostics.</summary>
    public string EventType { get; init; } = default!;

    /// <summary>When the handler completed, in UTC.</summary>
    public DateTime ConsumedAt { get; init; }
}
