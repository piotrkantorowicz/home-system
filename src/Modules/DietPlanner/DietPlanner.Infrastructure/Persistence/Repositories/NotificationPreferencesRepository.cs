namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class NotificationPreferencesRepository : INotificationPreferencesRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public NotificationPreferencesRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<NotificationPreferences?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _dbContext.NotificationPreferences.FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task AddAsync(NotificationPreferences preferences, CancellationToken ct = default)
        => await _dbContext.NotificationPreferences.AddAsync(preferences, ct);

    public void Update(NotificationPreferences preferences)
        => _dbContext.NotificationPreferences.Update(preferences);
}
