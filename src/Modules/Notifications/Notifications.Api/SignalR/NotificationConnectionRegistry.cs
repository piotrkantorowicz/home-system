namespace Notifications.Api.SignalR;

using System.Collections.Concurrent;

using Notifications.Application.Channels;

internal sealed class NotificationConnectionRegistry : INotificationConnectionRegistry
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);

    public void Track(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _counts.AddOrUpdate(userId, 1, static (_, current) => current + 1);
    }

    public void Untrack(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _counts.AddOrUpdate(userId, 0, static (_, current) => current - 1);

        if (_counts.TryGetValue(userId, out var value) && value <= 0)
            _counts.TryRemove(new KeyValuePair<string, int>(userId, value));
    }

    public bool IsOnline(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return _counts.TryGetValue(userId, out var count) && count > 0;
    }
}
