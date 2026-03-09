using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.Goals;

public interface IGoalService
{
    Task<GoalResponse?> GetAsync(string userId);
    Task<GoalResponse> UpsertAsync(string userId, UpsertGoalRequest request);
}

public class GoalService : IGoalService
{
    private readonly AppDbContext _db;

    public GoalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GoalResponse?> GetAsync(string userId)
    {
        var goal = await _db.UserGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.UserId == userId);

        return goal is not null ? GoalResponse.FromEntity(goal) : null;
    }

    public async Task<GoalResponse> UpsertAsync(string userId, UpsertGoalRequest request)
    {
        var goal = await _db.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userId);

        if (goal is null)
        {
            goal = new UserGoal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DailyCalorieTarget = request.DailyCalorieTarget,
                ProteinGrams = request.ProteinGrams,
                CarbsGrams = request.CarbsGrams,
                FatGrams = request.FatGrams,
                FiberGrams = request.FiberGrams,
                CreatedAt = DateTime.UtcNow,
            };

            _db.UserGoals.Add(goal);
        }
        else
        {
            goal.DailyCalorieTarget = request.DailyCalorieTarget;
            goal.ProteinGrams = request.ProteinGrams;
            goal.CarbsGrams = request.CarbsGrams;
            goal.FatGrams = request.FatGrams;
            goal.FiberGrams = request.FiberGrams;
            goal.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return GoalResponse.FromEntity(goal);
    }
}
