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
        => await _dbContext.MealEntries
            .Include(x => x.ActualProducts)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<MealEntry>> GetByPersonAndDateRangeAsync(
        Guid personId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
        => await _dbContext.MealEntries
            .Include(x => x.ActualProducts)
            .Where(x => x.PersonId == personId
                && (fromDate == null || x.Date >= fromDate)
                && (toDate == null || x.Date <= toDate))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.SequenceOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(DateOnly Date, MealSlotId MealSlotId)>> GetOccupiedSlotsAsync(
        Guid personId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var slots = await _dbContext.MealEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.Date >= fromDate && x.Date <= toDate)
            .Select(x => new { x.Date, x.MealSlotId })
            .Distinct()
            .ToListAsync(ct);

        return [.. slots.Select(x => (x.Date, x.MealSlotId))];
    }

    public async Task<bool> AnyForSlotAsync(MealSlotId mealSlotId, CancellationToken ct = default)
        => await _dbContext.MealEntries.AnyAsync(x => x.MealSlotId == mealSlotId, ct);

    public async Task AddAsync(MealEntry entry, CancellationToken ct = default)
        => await _dbContext.MealEntries.AddAsync(entry, ct);

    public void Update(MealEntry entry)
        => _dbContext.MealEntries.Update(entry);

    public void Delete(MealEntry entry)
        => _dbContext.MealEntries.Remove(entry);
}
