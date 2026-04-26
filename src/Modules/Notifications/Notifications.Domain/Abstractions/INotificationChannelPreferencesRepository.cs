namespace Notifications.Domain.Abstractions;

using Notifications.Domain.Models;

public interface INotificationChannelPreferencesRepository
{
    Task<NotificationChannelPreferences?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(NotificationChannelPreferences preferences, CancellationToken ct = default);
    Task UpdateAsync(NotificationChannelPreferences preferences, CancellationToken ct = default);
}
