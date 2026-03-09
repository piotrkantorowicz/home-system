using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.DietPlans;

public interface IMealEntryService
{
    Task<MealEntryDto> CreateAsync(Guid dietPlanId, string userId, CreateMealEntryRequest request);
    Task<MealEntryDto> UpdateAsync(Guid dietPlanId, Guid mealId, string userId, UpdateMealEntryRequest request);
    Task DeleteAsync(Guid dietPlanId, Guid mealId, string userId);
}

public class MealEntryService : IMealEntryService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MealEntryService> _logger;

    public MealEntryService(AppDbContext db, ILogger<MealEntryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MealEntryDto> CreateAsync(Guid dietPlanId, string userId, CreateMealEntryRequest request)
    {
        var plan = await _db.DietPlans.AsNoTracking().FirstOrDefaultAsync(dp => dp.Id == dietPlanId);

        if (plan == null)
            throw new NotFoundException("Diet plan", dietPlanId);

        if (plan.UserId != userId)
            throw new ForbiddenException("You can only add meals to your own diet plans");

        var recipe = await _db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RecipeId);

        if (recipe == null)
            throw new NotFoundException("Recipe", request.RecipeId);

        var entry = new MealEntry
        {
            Id = Guid.NewGuid(),
            DietPlanId = dietPlanId,
            Date = request.Date,
            MealType = request.MealType,
            RecipeId = request.RecipeId,
            Servings = request.Servings,
            Notes = request.Notes,
            MealTime = request.MealTime,
            SequenceOrder = request.SequenceOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.MealEntries.Add(entry);
        await _db.SaveChangesAsync();

        entry.Recipe = recipe;

        _logger.LogInformation("Meal entry created: {MealId} for plan {PlanId} by user {UserId}",
            entry.Id, dietPlanId, userId);

        return MealEntryDto.FromEntity(entry);
    }

    public async Task<MealEntryDto> UpdateAsync(Guid dietPlanId, Guid mealId, string userId, UpdateMealEntryRequest request)
    {
        var plan = await _db.DietPlans.AsNoTracking().FirstOrDefaultAsync(dp => dp.Id == dietPlanId);

        if (plan == null)
            throw new NotFoundException("Diet plan", dietPlanId);

        if (plan.UserId != userId)
            throw new ForbiddenException("You can only update meals in your own diet plans");

        var entry = await _db.MealEntries
            .Include(me => me.Recipe)
            .FirstOrDefaultAsync(me => me.Id == mealId && me.DietPlanId == dietPlanId);

        if (entry == null)
            throw new NotFoundException("Meal entry", mealId);

        if (entry.RecipeId != request.RecipeId)
        {
            var recipe = await _db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RecipeId);

            if (recipe == null)
                throw new NotFoundException("Recipe", request.RecipeId);

            entry.Recipe = recipe;
        }

        entry.Date = request.Date;
        entry.MealType = request.MealType;
        entry.RecipeId = request.RecipeId;
        entry.Servings = request.Servings;
        entry.Notes = request.Notes;
        entry.MealTime = request.MealTime;
        entry.SequenceOrder = request.SequenceOrder;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Meal entry updated: {MealId} in plan {PlanId} by user {UserId}",
            mealId, dietPlanId, userId);

        return MealEntryDto.FromEntity(entry);
    }

    public async Task DeleteAsync(Guid dietPlanId, Guid mealId, string userId)
    {
        var plan = await _db.DietPlans.AsNoTracking().FirstOrDefaultAsync(dp => dp.Id == dietPlanId);

        if (plan == null)
            throw new NotFoundException("Diet plan", dietPlanId);

        if (plan.UserId != userId)
            throw new ForbiddenException("You can only delete meals from your own diet plans");

        var entry = await _db.MealEntries
            .FirstOrDefaultAsync(me => me.Id == mealId && me.DietPlanId == dietPlanId);

        if (entry == null)
            throw new NotFoundException("Meal entry", mealId);

        _db.MealEntries.Remove(entry);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Meal entry deleted: {MealId} from plan {PlanId} by user {UserId}",
            mealId, dietPlanId, userId);
    }
}
