namespace Notifications.Domain.Abstractions;

/// <summary>
/// The module's own view of its <c>inbox_messages</c> table, kept for handlers that need to check
/// idempotency outside the shared Dapper inbox executor.
/// </summary>
public interface IInboxStore
{
    /// <summary>Whether the event was already consumed.</summary>
    /// <param name="eventId">The integration event's id.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default);
    /// <summary>Records the event as consumed.</summary>
    /// <param name="eventId">The integration event's id.</param>
    /// <param name="eventType">Assembly-qualified event type name, for diagnostics.</param>
    /// <param name="consumedAt">The current time, UTC.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task RecordAsync(Guid eventId, string eventType, DateTime consumedAt, CancellationToken ct = default);
}
