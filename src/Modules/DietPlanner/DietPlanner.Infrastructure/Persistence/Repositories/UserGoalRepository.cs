namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class UserGoalRepository : IUserGoalRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public UserGoalRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<UserGoal?> GetByPersonIdAsync(Guid personId, CancellationToken ct = default)
        => await _dbContext.UserGoals.FirstOrDefaultAsync(x => x.PersonId == personId, ct);

    public async Task AddAsync(UserGoal goal, CancellationToken ct = default)
        => await _dbContext.UserGoals.AddAsync(goal, ct);

    public void Update(UserGoal goal)
        => _dbContext.UserGoals.Update(goal);
}
