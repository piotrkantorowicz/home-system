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

        return await (
            from invitation in _db.HouseholdInvitations.AsNoTracking()
            where invitation.HouseholdId == householdId && invitation.Status == InvitationStatus.Pending
            orderby invitation.CreatedAt descending
            select new InvitationDto(
                invitation.Id.Value,
                invitation.Email == null ? null : invitation.Email.Value,
                invitation.TargetPersonId == null
                    ? null
                    : _db.Persons.Where(p => p.Id == invitation.TargetPersonId).Select(p => p.DisplayName).FirstOrDefault(),
                invitation.Role.ToString(),
                invitation.Status.ToString(),
                invitation.CreatedAt,
                invitation.ExpiresAt))
            .ToListAsync(ct);
    }
}
