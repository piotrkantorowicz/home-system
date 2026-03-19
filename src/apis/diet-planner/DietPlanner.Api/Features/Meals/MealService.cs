using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using DietPlanner.Api.Features.Recipes;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.Meals;

public interface IMealService
{
    Task<List<MealEntryDto>> GetAsync(string userId, DateOnly? from, DateOnly? to);
    Task<List<DailyNutritionDto>> GetNutritionSummaryAsync(string userId, DateOnly? from, DateOnly? to);
    Task<MealEntryDto> CreateAsync(string userId, CreateMealEntryRequest request);
    Task<MealEntryDto> UpdateAsync(Guid id, string userId, UpdateMealEntryRequest request);
    Task DeleteAsync(Guid id, string userId);
}

public class MealService : IMealService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MealService> _logger;
    private readonly INutritionCalculator _nutritionCalculator;

    public MealService(AppDbContext db, ILogger<MealService> logger, INutritionCalculator nutritionCalculator)
    {
        _db = db;
        _logger = logger;
        _nutritionCalculator = nutritionCalculator;
    }

    public async Task<List<MealEntryDto>> GetAsync(string userId, DateOnly? from, DateOnly? to)
    {
        var query = _db.MealEntries
            .Include(me => me.Recipe)
            .Where(me => me.UserId == userId);

        if (from.HasValue)
            query = query.Where(me => me.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(me => me.Date <= to.Value);

        var entries = await query
            .OrderBy(me => me.Date)
            .ThenBy(me => me.MealType)
            .ThenBy(me => me.SequenceOrder)
            .ToListAsync();

        return entries.Select(MealEntryDto.FromEntity).ToList();
    }

    public async Task<List<DailyNutritionDto>> GetNutritionSummaryAsync(string userId, DateOnly? from, DateOnly? to)
    {
        var query = _db.MealEntries
            .Include(me => me.Recipe)
                .ThenInclude(r => r.Ingredients)
                    .ThenInclude(i => i.Product)
            .Where(me => me.UserId == userId);

        if (from.HasValue)
            query = query.Where(me => me.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(me => me.Date <= to.Value);

        var entries = await query.ToListAsync();

        var result = entries
            .GroupBy(me => me.Date)
            .Select(g =>
            {
                decimal calories = 0, protein = 0, carbs = 0, fat = 0, fiber = 0;

                foreach (var entry in g)
                {
                    var nutrition = _nutritionCalculator.CalculateNutritionForServings(entry.Recipe, entry.Servings);
                    calories += nutrition.Calories;
                    protein += nutrition.Protein;
                    carbs += nutrition.Carbs;
                    fat += nutrition.Fat;
                    fiber += nutrition.Fiber;
                }

                return new DailyNutritionDto
                {
                    Date = g.Key,
                    Calories = Math.Round(calories, 1),
                    Protein = Math.Round(protein, 1),
                    Carbs = Math.Round(carbs, 1),
                    Fat = Math.Round(fat, 1),
                    Fiber = Math.Round(fiber, 1),
                };
            })
            .OrderBy(d => d.Date)
            .ToList();

        return result;
    }

    public async Task<MealEntryDto> CreateAsync(string userId, CreateMealEntryRequest request)
    {
        var recipe = await _db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RecipeId);

        if (recipe == null)
            throw new NotFoundException("Recipe", request.RecipeId);

        var entry = new MealEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
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

        _logger.LogInformation("Meal entry created: {MealId} for user {UserId}", entry.Id, userId);

        return MealEntryDto.FromEntity(entry);
    }

    public async Task<MealEntryDto> UpdateAsync(Guid id, string userId, UpdateMealEntryRequest request)
    {
        var entry = await _db.MealEntries
            .Include(me => me.Recipe)
            .FirstOrDefaultAsync(me => me.Id == id);

        if (entry == null)
            throw new NotFoundException("Meal entry", id);

        if (entry.UserId != userId)
            throw new ForbiddenException("You can only update your own meal entries");

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

        _logger.LogInformation("Meal entry updated: {MealId} by user {UserId}", id, userId);

        return MealEntryDto.FromEntity(entry);
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var entry = await _db.MealEntries.FirstOrDefaultAsync(me => me.Id == id);

        if (entry == null)
            throw new NotFoundException("Meal entry", id);

        if (entry.UserId != userId)
            throw new ForbiddenException("You can only delete your own meal entries");

        _db.MealEntries.Remove(entry);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Meal entry deleted: {MealId} by user {UserId}", id, userId);
    }
}
