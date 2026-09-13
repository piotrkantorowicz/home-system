namespace Household.Application.Queries.GetMyHousehold;

using Household.Application.Persistence;
using Household.Application.Queries.Projections;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetMyHouseholdQueryHandler : IQueryHandler<GetMyHouseholdQuery, MyHouseholdDto?>
{
    private readonly IHouseholdReadDbContext _db;

    public GetMyHouseholdQueryHandler(IHouseholdReadDbContext db) => _db = db;

    public async Task<MyHouseholdDto?> HandleAsync(GetMyHouseholdQuery query, CancellationToken ct)
    {
        var meId = await _db.Persons.AsNoTracking()
            .Where(p => p.AuthSubject == query.AuthSubject)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (meId is null)
            return null;

        var household = await _db.Households.AsNoTracking()
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Members.Any(m => m.PersonId == meId), ct);

        if (household is null)
            return null;

        var members = await MemberProjection.LoadAsync(_db, household.Members, ct);
        var myRole = household.Members.Single(m => m.PersonId == meId).Role;

        return new MyHouseholdDto(household.Id.Value, household.Name, myRole.ToString(), members);
    }
}
