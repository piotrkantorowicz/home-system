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
            .OrderBy(x => x.me.Date)
            .ThenBy(x => x.me.MealType)
            .ThenBy(x => x.me.SequenceOrder)
            .Select(x => new MealEntryDto(
                x.me.Id.Value,
                x.me.Date,
                x.me.MealType,
                x.me.RecipeId.Value,
                x.RecipeName,
                x.me.Servings,
                x.me.Notes,
                x.me.MealTime,
                x.me.SequenceOrder,
                x.me.CreatedAt))
            .ToListAsync(ct);
}
