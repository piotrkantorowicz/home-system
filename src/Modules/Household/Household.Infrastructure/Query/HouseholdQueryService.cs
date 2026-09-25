namespace Household.Infrastructure.Query;

using Household.Application.Persistence;
using Household.Contracts.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Read-side implementation of <see cref="IHouseholdQueryService"/>. Projects with
/// <c>AsNoTracking</c> — it never hydrates the <c>Household</c> aggregate for a write.
/// </summary>
internal sealed class HouseholdQueryService : IHouseholdQueryService
{
    private readonly IHouseholdReadDbContext _db;

    public HouseholdQueryService(IHouseholdReadDbContext db) => _db = db;

    public async Task<HouseholdContext?> GetHouseholdContextForUserAsync(
        string authSubject,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authSubject))
            return null;

        var meId = await _db.Persons.AsNoTracking()
            .Where(p => p.AuthSubject == authSubject)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (meId is null)
            return null;

        var household = await _db.Households.AsNoTracking()
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Members.Any(m => m.PersonId == meId), ct);

        if (household is null)
            return null;

        var personIds = household.Members.Select(m => m.PersonId).ToList();

        var persons = await _db.Persons.AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.DisplayName, p.IsManaged, p.AuthSubject })
            .ToDictionaryAsync(p => p.Id, ct);

        var members = household.Members
            .Select(m =>
            {
                var person = persons.GetValueOrDefault(m.PersonId);
                return new HouseholdContextMember(
                    m.PersonId.Value,
                    person?.DisplayName ?? "Unknown",
                    m.Role.ToString(),
                    person?.IsManaged ?? false,
                    person?.AuthSubject);
            })
            .OrderByDescending(m => m.Role == nameof(Domain.ValueObjects.HouseholdRole.Owner))
            .ThenBy(m => m.DisplayName)
            .ToList();

        var myRole = household.Members.Single(m => m.PersonId == meId).Role;

        return new HouseholdContext(household.Id.Value, meId.Value, myRole.ToString(), members);
    }
    public Task<string?> GetAuthSubjectForPersonAsync(Guid personId, CancellationToken ct = default)
        => _db.Persons.AsNoTracking()
            .Where(p => p.Id == Domain.ValueObjects.PersonId.From(personId))
            .Select(p => p.AuthSubject)
            .FirstOrDefaultAsync(ct);
}
