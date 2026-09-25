namespace DietPlanner.Application.Queries.GetMealSchedule;

using System.Globalization;
using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetMealScheduleQueryHandler : IQueryHandler<GetMealScheduleQuery, MealScheduleConfigDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetMealScheduleQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<MealScheduleConfigDto?> HandleAsync(GetMealScheduleQuery query, CancellationToken ct = default)
        => await _dbContext.MealScheduleConfigs
            .AsNoTracking()
            .Where(c => c.PersonId == query.PersonId)
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
