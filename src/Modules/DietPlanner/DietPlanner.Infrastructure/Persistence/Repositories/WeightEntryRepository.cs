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

    public async Task<WeightEntry?> GetByPersonAndDateAsync(Guid personId, DateOnly day, CancellationToken ct = default)
        => await _dbContext.WeightEntries
            .FirstOrDefaultAsync(x => x.PersonId == personId && x.Date == day, ct);

    public async Task<List<WeightEntry>> GetByPersonAsync(
        Guid personId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
        => await _dbContext.WeightEntries
            .Where(x => x.PersonId == personId
                && (fromDate == null || x.Date >= fromDate)
                && (toDate == null || x.Date <= toDate))
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

    public async Task<WeightEntry?> GetLatestByPersonAsync(
        Guid personId, WeightEntryId? excludeId = null, CancellationToken ct = default)
        => await _dbContext.WeightEntries
            .Where(x => x.PersonId == personId && (excludeId == null || x.Id != excludeId))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(WeightEntry entry, CancellationToken ct = default)
        => await _dbContext.WeightEntries.AddAsync(entry, ct);

    public void Delete(WeightEntry entry)
        => _dbContext.WeightEntries.Remove(entry);
}
