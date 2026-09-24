namespace Household.Application.Queries.ListMyInvitations;

using Household.Application.Persistence;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class ListMyInvitationsQueryHandler(IHouseholdReadDbContext db)
    : IQueryHandler<ListMyInvitationsQuery, IReadOnlyList<MyInvitationDto>>
{
    public async Task<IReadOnlyList<MyInvitationDto>> HandleAsync(
        ListMyInvitationsQuery query, CancellationToken ct)
    {
        var caller = await db.Persons.AsNoTracking()
            .Where(p => p.AuthSubject == query.AuthSubject)
            .Select(p => new { p.Id, Email = p.Email == null ? null : p.Email.Value })
            .FirstOrDefaultAsync(ct);

        if (caller is null)
            return [];

        return await (
            from invitation in db.HouseholdInvitations.AsNoTracking()
            join household in db.Households.AsNoTracking() on invitation.HouseholdId equals household.Id
            where invitation.Status == InvitationStatus.Pending
                  && (invitation.TargetPersonId == caller.Id
                      || (caller.Email != null && invitation.Email != null && invitation.Email.Value == caller.Email))
            orderby invitation.CreatedAt descending
            select new MyInvitationDto(
                invitation.Id.Value,
                household.Id.Value,
                household.Name,
                invitation.Role.ToString(),
                invitation.CreatedAt,
                invitation.ExpiresAt))
            .ToListAsync(ct);
    }
}
