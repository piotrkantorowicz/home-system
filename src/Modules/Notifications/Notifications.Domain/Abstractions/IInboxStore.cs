namespace Notifications.Domain.Abstractions;

public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default);
    Task RecordAsync(Guid eventId, string eventType, DateTime consumedAt, CancellationToken ct = default);
}
