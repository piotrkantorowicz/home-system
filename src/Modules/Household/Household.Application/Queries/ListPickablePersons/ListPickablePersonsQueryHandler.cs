namespace Household.Application.Queries.ListPickablePersons;

using Household.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class ListPickablePersonsQueryHandler
    : IQueryHandler<ListPickablePersonsQuery, IReadOnlyList<PickablePersonDto>>
{
    private readonly IHouseholdReadDbContext _db;

    public ListPickablePersonsQueryHandler(IHouseholdReadDbContext db) => _db = db;

    public async Task<IReadOnlyList<PickablePersonDto>> HandleAsync(
        ListPickablePersonsQuery query, CancellationToken ct)
    {
        var takenPersonIds = _db.Households.AsNoTracking().SelectMany(h => h.Members).Select(m => m.PersonId);

        return await _db.Persons.AsNoTracking()
            .Where(p => !takenPersonIds.Contains(p.Id))
            .Where(p => p.AuthSubject == null || p.AuthSubject != query.AuthSubject)
            .OrderBy(p => p.DisplayName)
            .Select(p => new PickablePersonDto(
                p.Id.Value,
                p.DisplayName,
                p.Email == null ? null : p.Email.Value,
                p.IsManaged))
            .ToListAsync(ct);
    }
}
