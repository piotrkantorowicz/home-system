namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

public interface IDietReminderSettingsRepository
{
    Task<DietReminderSettings?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(DietReminderSettings settings, CancellationToken ct = default);
    void Update(DietReminderSettings settings);
}
