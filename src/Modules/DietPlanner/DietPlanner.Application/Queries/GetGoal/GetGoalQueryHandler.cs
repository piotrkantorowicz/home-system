namespace DietPlanner.Application.Queries.GetGoal;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetGoalQueryHandler : IQueryHandler<GetGoalQuery, GoalDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetGoalQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<GoalDto?> HandleAsync(GetGoalQuery query, CancellationToken ct = default)
        => await _dbContext.UserGoals
            .AsNoTracking()
            .Where(g => g.PersonId == query.PersonId)
            .Select(g => new GoalDto(
                g.Id.Value,
                g.PersonId,
                g.DailyCalorieTarget,
                g.ProteinGrams,
                g.CarbsGrams,
                g.FatGrams,
                g.FiberGrams,
                g.CreatedAt,
                g.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
