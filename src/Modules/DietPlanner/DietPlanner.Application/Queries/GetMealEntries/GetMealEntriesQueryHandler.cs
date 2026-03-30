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
                me => me.RecipeId.Value,
                r => r.Id.Value,
                (me, r) => new MealEntryDto(
                    me.Id.Value,
                    me.Date,
                    me.MealType,
                    me.RecipeId.Value,
                    r.Name,
                    me.Servings,
                    me.Notes,
                    me.MealTime,
                    me.SequenceOrder,
                    me.CreatedAt))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.MealType)
            .ThenBy(x => x.SequenceOrder)
            .ToListAsync(ct);
}
