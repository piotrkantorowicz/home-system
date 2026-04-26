namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class DietReminderSettingsRepository : IDietReminderSettingsRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public DietReminderSettingsRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<DietReminderSettings?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _dbContext.DietReminderSettings.FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task AddAsync(DietReminderSettings settings, CancellationToken ct = default)
        => await _dbContext.DietReminderSettings.AddAsync(settings, ct);

    public void Update(DietReminderSettings settings)
        => _dbContext.DietReminderSettings.Update(settings);
}
