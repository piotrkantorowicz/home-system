namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

public interface IUserGoalRepository
{
    Task<UserGoal?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(UserGoal goal, CancellationToken ct = default);
    void Update(UserGoal goal);
}
