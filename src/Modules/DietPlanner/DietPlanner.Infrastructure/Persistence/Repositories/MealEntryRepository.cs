namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class MealEntryRepository : IMealEntryRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public MealEntryRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<MealEntry?> GetByIdAsync(MealEntryId id, CancellationToken ct = default)
        => await _dbContext.MealEntries.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<MealEntry>> GetByUserAndDateRangeAsync(
        string userId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
        => await _dbContext.MealEntries
            .Where(x => x.UserId == userId
                && (from == null || x.Date >= from)
                && (to == null || x.Date <= to))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.SequenceOrder)
            .ToListAsync(ct);

    public async Task<bool> AnyForSlotAsync(MealSlotId mealSlotId, CancellationToken ct = default)
        => await _dbContext.MealEntries.AnyAsync(x => x.MealSlotId == mealSlotId, ct);

    public async Task AddAsync(MealEntry entry, CancellationToken ct = default)
        => await _dbContext.MealEntries.AddAsync(entry, ct);

    public void Update(MealEntry entry)
        => _dbContext.MealEntries.Update(entry);

    public void Delete(MealEntry entry)
        => _dbContext.MealEntries.Remove(entry);
}
