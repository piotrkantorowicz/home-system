namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class MealScheduleConfigRepository : IMealScheduleConfigRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public MealScheduleConfigRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<MealScheduleConfig?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _dbContext.MealScheduleConfigs
            .Include(x => x.Slots)
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task AddAsync(MealScheduleConfig config, CancellationToken ct = default)
        => await _dbContext.MealScheduleConfigs.AddAsync(config, ct);

    public void Update(MealScheduleConfig config)
        => _dbContext.MealScheduleConfigs.Update(config);
}
