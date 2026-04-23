namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class WeightEntryRepository : IWeightEntryRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public WeightEntryRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<WeightEntry?> GetByIdAsync(WeightEntryId id, CancellationToken ct = default)
        => await _dbContext.WeightEntries.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<WeightEntry?> GetByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default)
        => await _dbContext.WeightEntries
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);

    public async Task<List<WeightEntry>> GetByUserAsync(
        string userId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
        => await _dbContext.WeightEntries
            .Where(x => x.UserId == userId
                && (from == null || x.Date >= from)
                && (to == null || x.Date <= to))
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

    public async Task<WeightEntry?> GetLatestByUserAsync(
        string userId, WeightEntryId? excludeId = null, CancellationToken ct = default)
        => await _dbContext.WeightEntries
            .Where(x => x.UserId == userId && (excludeId == null || x.Id != excludeId))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(WeightEntry entry, CancellationToken ct = default)
        => await _dbContext.WeightEntries.AddAsync(entry, ct);

    public void Delete(WeightEntry entry)
        => _dbContext.WeightEntries.Remove(entry);
}
