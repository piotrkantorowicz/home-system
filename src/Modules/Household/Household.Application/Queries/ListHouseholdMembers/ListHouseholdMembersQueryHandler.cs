namespace Household.Application.Queries.ListHouseholdMembers;

using Household.Application.Persistence;
using Household.Application.Queries.Projections;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ListHouseholdMembersQueryHandler
    : IQueryHandler<ListHouseholdMembersQuery, IReadOnlyList<HouseholdMemberDto>>
{
    private readonly IHouseholdReadDbContext _db;

    public ListHouseholdMembersQueryHandler(IHouseholdReadDbContext db) => _db = db;

    public async Task<IReadOnlyList<HouseholdMemberDto>> HandleAsync(
        ListHouseholdMembersQuery query, CancellationToken ct)
    {
        var meId = await _db.Persons.AsNoTracking()
            .Where(p => p.AuthSubject == query.AuthSubject)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);

        var householdId = HouseholdId.From(query.HouseholdId);

        var household = await _db.Households.AsNoTracking()
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == householdId, ct)
            ?? throw new NotFoundException("Household", query.HouseholdId);

        if (meId is null || household.Members.All(m => m.PersonId != meId))
            throw new ForbiddenException("You are not a member of this household.");

        return await MemberProjection.LoadAsync(_db, household.Members, ct);
    }
}
