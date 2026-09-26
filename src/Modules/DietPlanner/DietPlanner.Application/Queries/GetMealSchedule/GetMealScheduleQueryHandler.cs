namespace DietPlanner.Application.Queries.GetMealSchedule;

using System.Globalization;
using DietPlanner.Application.Households;
using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class GetMealScheduleQueryHandler(
    IDietPlannerReadDbContext dbContext,
    HouseholdRosterProvider households) : IQueryHandler<GetMealScheduleQuery, MealScheduleConfigDto?>
{
    public async Task<MealScheduleConfigDto?> HandleAsync(GetMealScheduleQuery query, CancellationToken ct = default)
    {
        var personId = query.ForPersonId ?? query.PersonId;
        HouseholdRoster roster = await households.GetAsync(query.PersonId, query.AuthSubject, ct);
        if (!roster.IsMember(personId))
            throw new ForbiddenException("You can only view the meal schedule of your own household members.");

        return await dbContext.MealScheduleConfigs
            .AsNoTracking()
            .Where(c => c.PersonId == personId)
            .Select(c => new MealScheduleConfigDto(
                c.Id.Value,
                c.PersonId,
                c.Slots
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new MealSlotDto(
                        s.Id.Value,
                        s.Name,
                        s.DefaultTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                        s.SortOrder))
                    .ToList(),
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
