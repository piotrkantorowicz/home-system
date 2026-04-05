namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

public interface INotificationPreferencesRepository
{
    Task<NotificationPreferences?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(NotificationPreferences preferences, CancellationToken ct = default);
    void Update(NotificationPreferences preferences);
}
