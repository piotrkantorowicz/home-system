namespace DietPlanner.Application.Queries.GetMealEntries;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetMealEntriesQueryHandler
    : IQueryHandler<GetMealEntriesQuery, IReadOnlyList<MealEntryDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetMealEntriesQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<IReadOnlyList<MealEntryDto>> HandleAsync(
        GetMealEntriesQuery query, CancellationToken ct = default)
        => await _dbContext.MealEntries
            .AsNoTracking()
            .Where(me => me.UserId == query.UserId
                && (query.From == null || me.Date >= query.From)
                && (query.To == null || me.Date <= query.To))
            .Join(_dbContext.Recipes.AsNoTracking().IgnoreQueryFilters(),
                me => me.RecipeId,
                r => r.Id,
                (me, r) => new { me, RecipeName = r.Name })
            .Join(_dbContext.MealSlots.AsNoTracking(),
                x => x.me.MealSlotId,
                s => s.Id,
                (x, s) => new
                {
                    x.me,
                    x.RecipeName,
                    SlotName = s.Name,
                    SlotDefaultTime = s.DefaultTime,
                    SlotSortOrder = s.SortOrder,
                })
            .OrderBy(x => x.me.Date)
            .ThenBy(x => x.SlotSortOrder)
            .ThenBy(x => x.me.SequenceOrder)
            .Select(x => new MealEntryDto(
                x.me.Id.Value,
                x.me.Date,
                x.me.MealSlotId.Value,
                x.SlotName,
                x.SlotDefaultTime,
                x.SlotSortOrder,
                x.me.RecipeId.Value,
                x.RecipeName,
                x.me.Servings,
                x.me.Notes,
                x.me.MealTime,
                x.me.SequenceOrder,
                x.me.CreatedAt))
            .ToListAsync(ct);
}
