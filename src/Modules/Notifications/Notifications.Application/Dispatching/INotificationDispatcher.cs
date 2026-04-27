namespace Notifications.Application.Dispatching;

using Notifications.Domain.ValueObjects;

public interface INotificationDispatcher
{
    Task DispatchAsync(
        NotificationType type,
        string userId,
        string locale,
        string payload,
        IReadOnlyDictionary<string, string>? placeholders,
        CancellationToken ct = default);
}
