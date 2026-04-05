namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class WaterIntakeRepository : IWaterIntakeRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public WaterIntakeRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<WaterIntake?> GetByIdAsync(WaterIntakeId id, CancellationToken ct = default)
        => await _dbContext.WaterIntakes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<WaterIntake>> GetByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default)
        => await _dbContext.WaterIntakes
            .Where(x => x.UserId == userId && x.Date == date)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(ct);

    public async Task AddAsync(WaterIntake intake, CancellationToken ct = default)
        => await _dbContext.WaterIntakes.AddAsync(intake, ct);

    public void Delete(WaterIntake intake)
        => _dbContext.WaterIntakes.Remove(intake);
}
