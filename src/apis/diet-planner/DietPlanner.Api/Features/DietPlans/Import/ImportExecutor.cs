using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using DietPlanner.Api.Features.Meals;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DietPlanner.Api.Features.DietPlans.Import;

public class ImportExecutor : IImportExecutor
{
    private readonly AppDbContext _db;
    private readonly ILogger<ImportExecutor> _logger;

    private static readonly HashSet<string> VolumeUnits = new(StringComparer.OrdinalIgnoreCase)
        { "ml", "l", "cup", "tbsp", "tsp" };

    public ImportExecutor(AppDbContext db, ILogger<ImportExecutor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ImportResultDto> ExecuteAsync(ImportDto import, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        var stats = new ImportStatsDto();

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Starting import execution for user {UserId}", userId);

                // Steps 1-3 stage all changes without intermediate saves.
                // EF Core generates GUIDs client-side on Add(), so IDs are available for relationships immediately.
                var productMap = await ProcessProductsAsync(import.Products, userId, stats);
                var recipeMap = await ProcessRecipesAsync(import.Recipes, userId, productMap, stats);
                await CreateMealEntriesAsync(import.Schedule, userId, recipeMap, stats);

                // Single save for the entire import
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                stopwatch.Stop();
                stats.DurationSeconds = stopwatch.Elapsed.TotalSeconds;

                _logger.LogInformation(
                    "Import completed in {Duration}s: {ProductsCreated} products created, {RecipesCreated} recipes created, {MealEntries} meal entries",
                    stats.DurationSeconds, stats.ProductsCreated, stats.RecipesCreated, stats.MealEntriesCreated);

                return new ImportResultDto
                {
                    Message = "Meals imported successfully",
                    Stats = stats
                };
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database constraint violation during import");
                throw new ImportException("Database constraint violation during import. Please check your data and try again.", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Import failed: {Message}", ex.Message);
                throw new ImportException($"Import failed: {ex.Message}", ex);
            }
        });
    }

    private async Task<Dictionary<string, Guid>> ProcessProductsAsync(
        List<ImportProductDto> importProducts,
        string userId,
        ImportStatsDto stats)
    {
        var productMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        if (!importProducts.Any())
            return productMap;

        var productNames = importProducts.Select(p => p.Name.ToLowerInvariant()).ToList();
        var existingProducts = await _db.Products
            .IgnoreQueryFilters()
            .Where(p => productNames.Contains(p.Name.ToLower()))
            .ToListAsync();

        foreach (var importProduct in importProducts)
        {
            if (VolumeUnits.Contains(importProduct.Unit) && !importProduct.DensityGramsPerMl.HasValue)
            {
                _logger.LogWarning(
                    "Product '{ProductName}' uses volume unit '{Unit}' but has no density. Defaulting to water density (1.0 g/ml).",
                    importProduct.Name, importProduct.Unit);
            }

            var existing = existingProducts
                .FirstOrDefault(p => p.Name.Equals(importProduct.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null && existing.CreatedByUserId == userId)
            {
                existing.CaloriesPer100g = importProduct.CaloriesPer100g;
                existing.ProteinPer100g = importProduct.ProteinPer100g;
                existing.CarbsPer100g = importProduct.CarbsPer100g;
                existing.FatPer100g = importProduct.FatPer100g;
                existing.FiberPer100g = importProduct.FiberPer100g;
                existing.DefaultUnit = importProduct.Unit;
                existing.DensityGramsPerMl = importProduct.DensityGramsPerMl;
                existing.GramPerPiece = importProduct.GramPerPiece;
                existing.UpdatedAt = DateTime.UtcNow;

                productMap[importProduct.Name] = existing.Id;
                stats.ProductsUpdated++;
                _logger.LogDebug("Staged product update: {ProductName}", importProduct.Name);
            }
            else if (existing != null)
            {
                productMap[importProduct.Name] = existing.Id;
                stats.ProductsReused++;
                _logger.LogDebug("Reusing existing product: {ProductName}", importProduct.Name);
            }
            else
            {
                var newProduct = new Product
                {
                    Name = importProduct.Name,
                    CaloriesPer100g = importProduct.CaloriesPer100g,
                    ProteinPer100g = importProduct.ProteinPer100g,
                    CarbsPer100g = importProduct.CarbsPer100g,
                    FatPer100g = importProduct.FatPer100g,
                    FiberPer100g = importProduct.FiberPer100g,
                    DefaultUnit = importProduct.Unit,
                    DensityGramsPerMl = importProduct.DensityGramsPerMl,
                    GramPerPiece = importProduct.GramPerPiece,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Products.Add(newProduct); // newProduct.Id is assigned by EF Core here
                productMap[importProduct.Name] = newProduct.Id;
                stats.ProductsCreated++;
                _logger.LogDebug("Staged new product: {ProductName}", importProduct.Name);
            }
        }

        return productMap;
    }

    private async Task<Dictionary<string, Guid>> ProcessRecipesAsync(
        List<ImportRecipeDto> importRecipes,
        string userId,
        Dictionary<string, Guid> productMap,
        ImportStatsDto stats)
    {
        var recipeMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        if (!importRecipes.Any())
            return recipeMap;

        var allIngredientProductNames = importRecipes
            .SelectMany(r => r.Ingredients.Select(i => i.Product.ToLowerInvariant()))
            .Distinct()
            .ToList();

        var allProducts = await _db.Products
            .IgnoreQueryFilters()
            .Where(p => allIngredientProductNames.Contains(p.Name.ToLower()))
            .ToListAsync();

        var recipeNames = importRecipes.Select(r => r.Name.ToLowerInvariant()).ToList();
        var existingRecipes = await _db.Recipes
            .IgnoreQueryFilters()
            .Include(r => r.Ingredients)
            .Where(r => recipeNames.Contains(r.Name.ToLower()))
            .ToListAsync();

        foreach (var importRecipe in importRecipes)
        {
            var existing = existingRecipes
                .FirstOrDefault(r => r.Name.Equals(importRecipe.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null && existing.CreatedByUserId == userId)
            {
                existing.Description = importRecipe.Description;
                existing.Instructions = importRecipe.Instructions;
                existing.Servings = importRecipe.Servings;
                existing.PrepTimeMinutes = importRecipe.PrepTimeMinutes;
                existing.UpdatedAt = DateTime.UtcNow;

                _db.RecipeIngredients.RemoveRange(existing.Ingredients);

                foreach (var ingredientDto in importRecipe.Ingredients)
                {
                    var product = ResolveProduct(ingredientDto.Product, allProducts, productMap)
                        ?? throw new ImportException($"Product '{ingredientDto.Product}' not found during recipe processing");

                    _db.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = existing.Id,
                        ProductId = product.Id,
                        Amount = ingredientDto.Amount,
                        Unit = ingredientDto.Unit
                    });
                }

                recipeMap[importRecipe.Name] = existing.Id;
                stats.RecipesUpdated++;
                _logger.LogDebug("Staged recipe update: {RecipeName}", importRecipe.Name);
            }
            else if (existing != null)
            {
                recipeMap[importRecipe.Name] = existing.Id;
                stats.RecipesReused++;
                _logger.LogDebug("Reusing existing recipe: {RecipeName}", importRecipe.Name);
            }
            else
            {
                var newRecipe = new Recipe
                {
                    Name = importRecipe.Name,
                    Description = importRecipe.Description,
                    Instructions = importRecipe.Instructions,
                    Servings = importRecipe.Servings,
                    PrepTimeMinutes = importRecipe.PrepTimeMinutes,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Recipes.Add(newRecipe); // newRecipe.Id is assigned by EF Core here

                foreach (var ingredientDto in importRecipe.Ingredients)
                {
                    var product = ResolveProduct(ingredientDto.Product, allProducts, productMap)
                        ?? throw new ImportException($"Product '{ingredientDto.Product}' not found during recipe processing");

                    _db.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = newRecipe.Id,
                        ProductId = product.Id,
                        Amount = ingredientDto.Amount,
                        Unit = ingredientDto.Unit
                    });
                }

                recipeMap[importRecipe.Name] = newRecipe.Id;
                stats.RecipesCreated++;
                _logger.LogDebug("Staged new recipe: {RecipeName}", importRecipe.Name);
            }
        }

        return recipeMap;
    }

    /// Resolves a product by name from the in-memory list (for existing DB products)
    /// or from the productMap (for newly staged products whose ID was assigned by EF Core).
    private Product? ResolveProduct(
        string productName,
        List<Product> dbProducts,
        Dictionary<string, Guid> productMap)
    {
        var dbProduct = dbProducts.FirstOrDefault(p =>
            p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

        if (dbProduct != null)
            return dbProduct;

        // For newly created products (not yet saved), the ID is in productMap but the Product
        // entity is tracked by EF Core — look it up from the change tracker
        if (productMap.TryGetValue(productName, out var productId))
        {
            var entry = _db.ChangeTracker.Entries<Product>()
                .FirstOrDefault(e => e.Entity.Id == productId);
            return entry?.Entity;
        }

        return null;
    }

    private async Task CreateMealEntriesAsync(
        List<ImportScheduleDto> schedule,
        string userId,
        Dictionary<string, Guid> recipeMap,
        ImportStatsDto stats)
    {
        var allRecipeNames = schedule
            .SelectMany(s => s.Meals.Select(m => m.Recipe.ToLowerInvariant()))
            .Distinct()
            .ToList();

        var allDbRecipes = await _db.Recipes
            .IgnoreQueryFilters()
            .Where(r => allRecipeNames.Contains(r.Name.ToLower()))
            .ToListAsync();

        var mealEntries = new List<MealEntry>();

        foreach (var scheduleEntry in schedule)
        {
            foreach (var meal in scheduleEntry.Meals)
            {
                Guid recipeId;
                if (recipeMap.TryGetValue(meal.Recipe, out var mappedId))
                {
                    recipeId = mappedId;
                }
                else
                {
                    var recipe = allDbRecipes.FirstOrDefault(r =>
                        r.Name.Equals(meal.Recipe, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ImportException($"Recipe '{meal.Recipe}' not found during meal entry creation");

                    recipeId = recipe.Id;
                }

                mealEntries.Add(new MealEntry
                {
                    UserId = userId,
                    Date = scheduleEntry.Date,
                    MealType = meal.Type.ToLowerInvariant(),
                    RecipeId = recipeId,
                    Servings = meal.Servings,
                    Notes = meal.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.MealEntries.AddRangeAsync(mealEntries);
        stats.MealEntriesCreated = mealEntries.Count;
        _logger.LogInformation("Staged {Count} meal entries", mealEntries.Count);
    }
}
