namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class HydrationConfigRepository : IHydrationConfigRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public HydrationConfigRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<HydrationConfig?> GetByPersonIdAsync(Guid personId, CancellationToken ct = default)
        => await _dbContext.HydrationConfigs.FirstOrDefaultAsync(x => x.PersonId == personId, ct);

    public async Task AddAsync(HydrationConfig config, CancellationToken ct = default)
        => await _dbContext.HydrationConfigs.AddAsync(config, ct);

    public void Update(HydrationConfig config)
        => _dbContext.HydrationConfigs.Update(config);
}
