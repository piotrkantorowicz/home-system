using Household.Application.Persistence;
using Household.Domain.Entities;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Household.Application.Queries.Projections;

/// <summary>
/// Joins a household's members to their <c>Person</c> records for display. Kept in one place
/// because both <c>GetMyHousehold</c> and <c>ListHouseholdMembers</c> need it.
/// </summary>
internal static class MemberProjection
{
    public static async Task<IReadOnlyList<HouseholdMemberDto>> LoadAsync(
        IHouseholdReadDbContext db,
        IEnumerable<HouseholdMember> members,
        CancellationToken ct)
    {
        var memberList = members.ToList();
        var personIds = memberList.Select(m => m.PersonId).ToList();

        var persons = await db.Persons.AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.DisplayName, p.AvatarUrl, p.IsManaged })
            .ToDictionaryAsync(p => p.Id, ct);

        return memberList
            .Select(m =>
            {
                var person = persons.GetValueOrDefault(m.PersonId);
                return new HouseholdMemberDto(
                    m.PersonId.Value,
                    person?.DisplayName ?? "Unknown",
                    person?.AvatarUrl,
                    m.Role.ToString(),
                    m.Nickname,
                    person?.IsManaged ?? false);
            })
            .OrderByDescending(m => m.Role == nameof(HouseholdRole.Owner))
            .ThenBy(m => m.DisplayName)
            .ToList();
    }
}
