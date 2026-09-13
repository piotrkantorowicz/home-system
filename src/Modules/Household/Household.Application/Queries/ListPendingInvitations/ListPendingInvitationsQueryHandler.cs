namespace Household.Application.Queries.ListPendingInvitations;

using Household.Application.Persistence;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ListPendingInvitationsQueryHandler
    : IQueryHandler<ListPendingInvitationsQuery, IReadOnlyList<InvitationDto>>
{
    private readonly IHouseholdReadDbContext _db;

    public ListPendingInvitationsQueryHandler(IHouseholdReadDbContext db) => _db = db;

    public async Task<IReadOnlyList<InvitationDto>> HandleAsync(
        ListPendingInvitationsQuery query, CancellationToken ct)
    {
        var householdId = HouseholdId.From(query.HouseholdId);

        var meId = await _db.Persons.AsNoTracking()
            .Where(p => p.AuthSubject == query.AuthSubject)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);

        var isMember = meId is not null && await _db.Households.AsNoTracking()
            .AnyAsync(h => h.Id == householdId && h.Members.Any(m => m.PersonId == meId), ct);

        if (!isMember)
            throw new ForbiddenException("You are not a member of this household.");

        return await _db.HouseholdInvitations.AsNoTracking()
            .Where(i => i.HouseholdId == householdId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationDto(
                i.Id.Value,
                i.Email!.Value,
                i.Role.ToString(),
                i.Status.ToString(),
                i.CreatedAt,
                i.ExpiresAt))
            .ToListAsync(ct);
    }
}
