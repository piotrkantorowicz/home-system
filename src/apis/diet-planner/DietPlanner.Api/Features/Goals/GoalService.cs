using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.Goals;

public interface IGoalService
{
    Task<GoalResponse?> GetAsync(string userId);
    Task<GoalResponse> CreateAsync(string userId, CreateGoalRequest request);
    Task<GoalResponse> UpdateAsync(string userId, UpdateGoalRequest request);
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

    public async Task<GoalResponse> CreateAsync(string userId, CreateGoalRequest request)
    {
        var existing = await _db.UserGoals
            .AnyAsync(g => g.UserId == userId);

        if (existing)
        {
            throw new ValidationException("Goals already exist for this user. Use PUT to update.");
        }

        var goal = new UserGoal
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
        await _db.SaveChangesAsync();

        return GoalResponse.FromEntity(goal);
    }

    public async Task<GoalResponse> UpdateAsync(string userId, UpdateGoalRequest request)
    {
        var goal = await _db.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userId)
            ?? throw new NotFoundException("UserGoal", userId);

        goal.DailyCalorieTarget = request.DailyCalorieTarget;
        goal.ProteinGrams = request.ProteinGrams;
        goal.CarbsGrams = request.CarbsGrams;
        goal.FatGrams = request.FatGrams;
        goal.FiberGrams = request.FiberGrams;
        goal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return GoalResponse.FromEntity(goal);
    }
}
