using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Household.Infrastructure.Persistence.Repositories;

internal sealed class HouseholdInvitationRepository : IHouseholdInvitationRepository
{
    private readonly HouseholdDbContext _dbContext;

    public HouseholdInvitationRepository(HouseholdDbContext dbContext) => _dbContext = dbContext;

    public Task<HouseholdInvitation?> GetByIdAsync(HouseholdInvitationId id, CancellationToken ct = default)
        => _dbContext.HouseholdInvitations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<HouseholdInvitation?> GetPendingByEmailAsync(PersonEmail email, CancellationToken ct = default)
        => _dbContext.HouseholdInvitations
            .Where(i => i.Status == InvitationStatus.Pending && i.Email!.Value == email.Value)
            .OrderBy(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<HouseholdInvitation>> ListForHouseholdAsync(
        HouseholdId householdId, CancellationToken ct = default)
        => await _dbContext.HouseholdInvitations
            .Where(i => i.HouseholdId == householdId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> HasPendingForEmailInHouseholdAsync(
        HouseholdId householdId, PersonEmail email, CancellationToken ct = default)
        => _dbContext.HouseholdInvitations.AnyAsync(
            i => i.HouseholdId == householdId
                 && i.Status == InvitationStatus.Pending
                 && i.Email!.Value == email.Value,
            ct);

    public async Task AddAsync(HouseholdInvitation invitation, CancellationToken ct = default)
        => await _dbContext.HouseholdInvitations.AddAsync(invitation, ct);
}
