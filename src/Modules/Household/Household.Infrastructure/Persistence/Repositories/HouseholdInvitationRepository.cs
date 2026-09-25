namespace Household.Infrastructure.Persistence.Repositories;

using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class HouseholdInvitationRepository : IHouseholdInvitationRepository
{
    private readonly HouseholdDbContext _dbContext;

    public HouseholdInvitationRepository(HouseholdDbContext dbContext) => _dbContext = dbContext;

    public Task<HouseholdInvitation?> GetByIdAsync(HouseholdInvitationId id, CancellationToken ct = default)
        => _dbContext.HouseholdInvitations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<HouseholdInvitation>> ListForHouseholdAsync(
        HouseholdId householdId, CancellationToken ct = default)
        => await _dbContext.HouseholdInvitations
            .Where(i => i.HouseholdId == householdId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> HasPendingForEmailInHouseholdAsync(
        HouseholdId householdId, PersonEmail email, DateTime now, CancellationToken ct = default)
        => _dbContext.HouseholdInvitations.AnyAsync(
            i => i.HouseholdId == householdId
                 && i.Status == InvitationStatus.Pending
                 && i.ExpiresAt > now
                 && i.Email != null && i.Email.Value == email.Value,
            ct);

    public Task<bool> HasPendingForPersonInHouseholdAsync(
        HouseholdId householdId, PersonId personId, DateTime now, CancellationToken ct = default)
        => _dbContext.HouseholdInvitations.AnyAsync(
            i => i.HouseholdId == householdId
                 && i.Status == InvitationStatus.Pending
                 && i.ExpiresAt > now
                 && i.TargetPersonId == personId,
            ct);

    public async Task AddAsync(HouseholdInvitation invitation, CancellationToken ct = default)
        => await _dbContext.HouseholdInvitations.AddAsync(invitation, ct);
}
