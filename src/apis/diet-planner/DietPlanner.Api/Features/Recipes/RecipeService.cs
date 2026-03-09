using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Common.Models;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.Recipes;

public interface IRecipeService
{
    Task<PagedResult<RecipeResponse>> SearchAsync(
        string? search,
        bool onlyMine,
        string userId,
        int page,
        int pageSize);

    Task<RecipeResponse> GetByIdAsync(Guid id, string userId);
    Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, string userId);
    Task<RecipeResponse> UpdateAsync(Guid id, UpdateRecipeRequest request, string userId);
    Task DeleteAsync(Guid id, string userId, bool permanent = false);
}

public class RecipeService : IRecipeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RecipeService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly INutritionCalculator _nutritionCalculator;

    public RecipeService(
        AppDbContext db,
        ILogger<RecipeService> logger,
        IWebHostEnvironment env,
        INutritionCalculator nutritionCalculator)
    {
        _db = db;
        _logger = logger;
        _env = env;
        _nutritionCalculator = nutritionCalculator;
    }

    public async Task<PagedResult<RecipeResponse>> SearchAsync(
        string? search,
        bool onlyMine,
        string userId,
        int page,
        int pageSize)
    {
        var query = _db.Recipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Product)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => EF.Functions.ILike(r.Name, $"%{search}%"));
        }

        if (onlyMine)
        {
            query = query.Where(r => r.CreatedByUserId == userId);
        }

        var totalCount = await query.CountAsync();

        var recipes = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = recipes.Select(recipe =>
        {
            var totalNutrition = _nutritionCalculator.CalculateTotalNutrition(recipe);
            var perServingNutrition = _nutritionCalculator.CalculateNutritionPerServing(recipe);
            return RecipeResponse.FromEntity(recipe, userId, totalNutrition, perServingNutrition);
        }).ToList();

        return new PagedResult<RecipeResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<RecipeResponse> GetByIdAsync(Guid id, string userId)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
        {
            throw new NotFoundException("Recipe", id);
        }

        var totalNutrition = _nutritionCalculator.CalculateTotalNutrition(recipe);
        var perServingNutrition = _nutritionCalculator.CalculateNutritionPerServing(recipe);

        return RecipeResponse.FromEntity(recipe, userId, totalNutrition, perServingNutrition);
    }

    public async Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, string userId)
    {
        var existingRecipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower());

        if (existingRecipe != null)
        {
            throw new ValidationException($"A recipe with the name '{request.Name}' already exists");
        }

        var productNames = request.Ingredients.Select(i => i.ProductName.ToLower()).ToList();
        var products = await _db.Products
            .Where(p => productNames.Contains(p.Name.ToLower()))
            .ToListAsync();

        var foundProductNames = products.Select(p => p.Name.ToLower()).ToList();
        var missingProducts = request.Ingredients
            .Where(i => !foundProductNames.Contains(i.ProductName.ToLower()))
            .Select(i => i.ProductName)
            .ToList();

        if (missingProducts.Any())
        {
            throw new ValidationException(
                $"The following products were not found or are deleted: {string.Join(", ", missingProducts)}");
        }

        var recipe = new Recipe
        {
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            Servings = request.Servings,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Recipes.Add(recipe);

        // EF Core assigns recipe.Id when Add() is called — no intermediate SaveChanges needed
        foreach (var ingredientRequest in request.Ingredients)
        {
            var product = products.First(p =>
                p.Name.Equals(ingredientRequest.ProductName, StringComparison.OrdinalIgnoreCase));

            _db.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                ProductId = product.Id,
                Amount = ingredientRequest.Amount,
                Unit = ingredientRequest.Unit
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Recipe created: {RecipeId} by user {UserId}", recipe.Id, userId);

        recipe = await _db.Recipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstAsync(r => r.Id == recipe.Id);

        var totalNutrition = _nutritionCalculator.CalculateTotalNutrition(recipe);
        var perServingNutrition = _nutritionCalculator.CalculateNutritionPerServing(recipe);

        return RecipeResponse.FromEntity(recipe, userId, totalNutrition, perServingNutrition);
    }

    public async Task<RecipeResponse> UpdateAsync(Guid id, UpdateRecipeRequest request, string userId)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
        {
            throw new NotFoundException("Recipe", id);
        }

        if (recipe.CreatedByUserId != userId)
        {
            throw new ForbiddenException("You can only update recipes you created");
        }

        if (recipe.Name.ToLower() != request.Name.ToLower())
        {
            var nameExists = await _db.Recipes
                .AnyAsync(r => r.Id != id && r.Name.ToLower() == request.Name.ToLower());

            if (nameExists)
            {
                throw new ValidationException($"A recipe with the name '{request.Name}' already exists");
            }
        }

        var productNames = request.Ingredients.Select(i => i.ProductName.ToLower()).ToList();
        var products = await _db.Products
            .Where(p => productNames.Contains(p.Name.ToLower()))
            .ToListAsync();

        var foundProductNames = products.Select(p => p.Name.ToLower()).ToList();
        var missingProducts = request.Ingredients
            .Where(i => !foundProductNames.Contains(i.ProductName.ToLower()))
            .Select(i => i.ProductName)
            .ToList();

        if (missingProducts.Any())
        {
            throw new ValidationException(
                $"The following products were not found or are deleted: {string.Join(", ", missingProducts)}");
        }

        recipe.Name = request.Name;
        recipe.Description = request.Description;
        recipe.Instructions = request.Instructions;
        recipe.Servings = request.Servings;
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.UpdatedAt = DateTime.UtcNow;

        _db.RecipeIngredients.RemoveRange(recipe.Ingredients);

        var newIngredients = request.Ingredients.Select(ir =>
        {
            var product = products.First(p =>
                p.Name.Equals(ir.ProductName, StringComparison.OrdinalIgnoreCase));

            return new RecipeIngredient
            {
                RecipeId = recipe.Id,
                ProductId = product.Id,
                Amount = ir.Amount,
                Unit = ir.Unit
            };
        }).ToList();

        _db.RecipeIngredients.AddRange(newIngredients);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Recipe updated: {RecipeId} by user {UserId}", recipe.Id, userId);

        recipe = await _db.Recipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstAsync(r => r.Id == id);

        var totalNutrition = _nutritionCalculator.CalculateTotalNutrition(recipe);
        var perServingNutrition = _nutritionCalculator.CalculateNutritionPerServing(recipe);

        return RecipeResponse.FromEntity(recipe, userId, totalNutrition, perServingNutrition);
    }

    public async Task DeleteAsync(Guid id, string userId, bool permanent = false)
    {
        // Use IgnoreQueryFilters to find soft-deleted recipes for permanent deletion
        var recipe = await _db.Recipes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
        {
            throw new NotFoundException("Recipe", id);
        }

        if (!permanent && recipe.DeletedAt != null)
        {
            throw new NotFoundException("Recipe", id);
        }

        if (recipe.CreatedByUserId != userId)
        {
            throw new ForbiddenException("You can only delete recipes you created");
        }

        if (permanent)
        {
            if (_env.IsProduction())
            {
                throw new ForbiddenException("Permanent deletion is not allowed in production environment");
            }

            var isUsedInMealEntries = await _db.MealEntries
                .AnyAsync(me => me.RecipeId == id);

            if (isUsedInMealEntries)
            {
                throw new ValidationException(
                    "Cannot permanently delete recipe that is used in meal entries. " +
                    "Remove it from meal entries first or use soft delete.");
            }

            _db.Recipes.Remove(recipe);
            _logger.LogInformation("Recipe permanently deleted: {RecipeId} by user {UserId}", recipe.Id, userId);
        }
        else
        {
            recipe.DeletedAt = DateTime.UtcNow;
            _logger.LogInformation("Recipe soft-deleted: {RecipeId} by user {UserId}", recipe.Id, userId);
        }

        await _db.SaveChangesAsync();
    }
}
