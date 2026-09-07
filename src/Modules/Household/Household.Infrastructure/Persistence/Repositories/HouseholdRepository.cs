using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

namespace Household.Infrastructure.Persistence.Repositories;

internal sealed class HouseholdRepository : IHouseholdRepository
{
    private readonly HouseholdDbContext _dbContext;

    public HouseholdRepository(HouseholdDbContext dbContext) => _dbContext = dbContext;

    public Task<HouseholdAggregate?> GetByIdAsync(HouseholdId id, CancellationToken ct = default)
        => _dbContext.Households
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<HouseholdAggregate?> GetByMemberPersonIdAsync(PersonId personId, CancellationToken ct = default)
        => _dbContext.Households
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Members.Any(m => m.PersonId == personId), ct);

    public async Task AddAsync(HouseholdAggregate household, CancellationToken ct = default)
        => await _dbContext.Households.AddAsync(household, ct);
}
