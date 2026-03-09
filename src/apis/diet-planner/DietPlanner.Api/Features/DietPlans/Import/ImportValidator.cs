using DietPlanner.Api.Common.Utils;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.DietPlans.Import;

public class ImportValidator : IImportValidator
{
    private readonly AppDbContext _db;
    private readonly ILogger<ImportValidator> _logger;

    public ImportValidator(AppDbContext db, ILogger<ImportValidator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ValidationResultDto> ValidateAsync(ImportDto import, string userId)
    {
        var result = new ValidationResultDto
        {
            Valid = true,
            CanProceed = true,
            Plan = new ImportPlanDto
            {
                DateRange = $"{import.StartDate:yyyy-MM-dd} to {import.EndDate:yyyy-MM-dd}"
            }
        };

        var issues = new List<ValidationIssueDto>();

        ValidateSchema(import, issues);
        ValidateReferentialIntegrity(import, issues);
        ValidateBusinessRules(import, issues);
        await ValidateConflictsAsync(import, userId, issues, result.Plan);
        CalculatePlanStatistics(import, result.Plan);

        result.Issues = issues;
        result.Summary = new ValidationSummaryDto
        {
            Errors = issues.Count(i => i.Severity == "error"),
            Warnings = issues.Count(i => i.Severity == "warning"),
            Info = issues.Count(i => i.Severity == "info")
        };

        result.Valid = result.Summary.Errors == 0;
        result.CanProceed = result.Valid;

        _logger.LogInformation(
            "Import validation completed: {Errors} errors, {Warnings} warnings, {Info} info",
            result.Summary.Errors, result.Summary.Warnings, result.Summary.Info);

        return result;
    }

    private void ValidateSchema(ImportDto import, List<ValidationIssueDto> issues)
    {
        if (string.IsNullOrWhiteSpace(import.PlanName))
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "error",
                Category = "schema",
                Path = "planName",
                Message = "Plan name is required",
                Resolution = "Provide a name for your diet plan"
            });
        }
        else if (import.PlanName.Length > 200)
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "error",
                Category = "schema",
                Path = "planName",
                Message = "Plan name cannot exceed 200 characters",
                Resolution = "Shorten the plan name"
            });
        }

        if (import.EndDate < import.StartDate)
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "error",
                Category = "schema",
                Path = "endDate",
                Message = "End date must be on or after start date",
                Resolution = "Adjust the date range"
            });
        }

        if (import.Schedule == null || !import.Schedule.Any())
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "error",
                Category = "schema",
                Path = "schedule",
                Message = "Schedule is required and must contain at least one day",
                Resolution = "Add at least one day to your schedule"
            });
            return;
        }

        for (int i = 0; i < import.Products.Count; i++)
        {
            var product = import.Products[i];
            var path = $"products[{i}]";

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "schema",
                    Path = $"{path}.name",
                    Message = "Product name is required",
                    Resolution = "Provide a name for the product"
                });
            }
            else if (product.Name.Length > 200)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "schema",
                    Path = $"{path}.name",
                    Item = product.Name,
                    Message = "Product name cannot exceed 200 characters",
                    Resolution = "Shorten the product name"
                });
            }

            if (product.CaloriesPer100g.HasValue && (product.CaloriesPer100g < 0 || product.CaloriesPer100g >= 9000))
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.caloriesPer100g",
                    Item = product.Name,
                    Message = "Calories per 100g must be between 0 and 9000",
                    Resolution = "Provide a valid calorie value"
                });
            }

            if (product.ProteinPer100g.HasValue && (product.ProteinPer100g < 0 || product.ProteinPer100g > 100))
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.proteinPer100g",
                    Item = product.Name,
                    Message = "Protein per 100g must be between 0 and 100",
                    Resolution = "Provide a valid protein value"
                });
            }

            if (product.CarbsPer100g.HasValue && (product.CarbsPer100g < 0 || product.CarbsPer100g > 100))
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.carbsPer100g",
                    Item = product.Name,
                    Message = "Carbs per 100g must be between 0 and 100",
                    Resolution = "Provide a valid carbs value"
                });
            }

            if (product.FatPer100g.HasValue && (product.FatPer100g < 0 || product.FatPer100g > 100))
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.fatPer100g",
                    Item = product.Name,
                    Message = "Fat per 100g must be between 0 and 100",
                    Resolution = "Provide a valid fat value"
                });
            }

            // Nutrition consistency check: calories should roughly match macro sum
            if (product.CaloriesPer100g.HasValue &&
                (product.ProteinPer100g.HasValue || product.CarbsPer100g.HasValue || product.FatPer100g.HasValue))
            {
                var calculatedCalories =
                    (product.ProteinPer100g ?? 0) * 4 +
                    (product.CarbsPer100g ?? 0) * 4 +
                    (product.FatPer100g ?? 0) * 9;

                if (Math.Abs(calculatedCalories - product.CaloriesPer100g.Value) > 15)
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "warning",
                        Category = "validation",
                        Path = $"{path}.caloriesPer100g",
                        Item = product.Name,
                        Message = $"Calorie value ({product.CaloriesPer100g}) doesn't match macro sum " +
                                  $"({calculatedCalories:F1} kcal from {product.ProteinPer100g ?? 0}g P + " +
                                  $"{product.CarbsPer100g ?? 0}g C + {product.FatPer100g ?? 0}g F)",
                        Resolution = "Verify nutrition values are correct (protein×4 + carbs×4 + fat×9 ≈ calories)"
                    });
                }
            }

            if (!UnitConverter.IsValidUnit(product.Unit))
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.unit",
                    Item = product.Name,
                    Message = $"Invalid unit '{product.Unit}'",
                    Resolution = "Use one of: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece"
                });
            }

            if (product.DensityGramsPerMl.HasValue && product.DensityGramsPerMl <= 0)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.densityGramsPerMl",
                    Item = product.Name,
                    Message = "Density must be positive",
                    Resolution = "Provide a valid density value or omit the field"
                });
            }

            if (product.GramPerPiece.HasValue && product.GramPerPiece <= 0)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.gramPerPiece",
                    Item = product.Name,
                    Message = "Grams per piece must be positive",
                    Resolution = "Provide a valid value or omit the field"
                });
            }

            // Warn if volume unit is used without density
            var volumeUnits = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ml", "l", "cup", "tbsp", "tsp" };
            if (volumeUnits.Contains(product.Unit) && !product.DensityGramsPerMl.HasValue)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "warning",
                    Category = "validation",
                    Path = $"{path}.densityGramsPerMl",
                    Item = product.Name,
                    Message = $"Product uses volume unit '{product.Unit}' but has no density specified",
                    Resolution = "Add densityGramsPerMl for accurate nutrition calculation (defaults to water: 1.0 g/ml)"
                });
            }
        }

        for (int i = 0; i < import.Recipes.Count; i++)
        {
            var recipe = import.Recipes[i];
            var path = $"recipes[{i}]";

            if (string.IsNullOrWhiteSpace(recipe.Name))
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "schema",
                    Path = $"{path}.name",
                    Message = "Recipe name is required",
                    Resolution = "Provide a name for the recipe"
                });
            }
            else if (recipe.Name.Length > 200)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "schema",
                    Path = $"{path}.name",
                    Item = recipe.Name,
                    Message = "Recipe name cannot exceed 200 characters",
                    Resolution = "Shorten the recipe name"
                });
            }

            if (recipe.Servings < 1 || recipe.Servings > 100)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.servings",
                    Item = recipe.Name,
                    Message = "Servings must be between 1 and 100",
                    Resolution = "Adjust the number of servings"
                });
            }

            if (recipe.PrepTimeMinutes.HasValue && recipe.PrepTimeMinutes < 0)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.prepTimeMinutes",
                    Item = recipe.Name,
                    Message = "Prep time cannot be negative",
                    Resolution = "Provide a valid prep time or omit the field"
                });
            }

            if (!recipe.Ingredients.Any())
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.ingredients",
                    Item = recipe.Name,
                    Message = "Recipe must have at least one ingredient",
                    Resolution = "Add ingredients to the recipe"
                });
            }

            var duplicateIngredients = recipe.Ingredients
                .GroupBy(ing => ing.Product.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIngredients.Any())
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.ingredients",
                    Item = recipe.Name,
                    Message = $"Recipe has duplicate ingredients: {string.Join(", ", duplicateIngredients)}",
                    Resolution = "Remove duplicate ingredients from the recipe"
                });
            }

            for (int j = 0; j < recipe.Ingredients.Count; j++)
            {
                var ingredient = recipe.Ingredients[j];
                var ingPath = $"{path}.ingredients[{j}]";

                if (ingredient.Amount <= 0)
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "validation",
                        Path = $"{ingPath}.amount",
                        Item = $"{recipe.Name} - {ingredient.Product}",
                        Message = "Ingredient amount must be positive",
                        Resolution = "Provide a valid amount"
                    });
                }

                if (!UnitConverter.IsValidUnit(ingredient.Unit))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "validation",
                        Path = $"{ingPath}.unit",
                        Item = $"{recipe.Name} - {ingredient.Product}",
                        Message = $"Invalid unit '{ingredient.Unit}'",
                        Resolution = "Use one of: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece"
                    });
                }
            }
        }

        for (int i = 0; i < import.Schedule.Count; i++)
        {
            var scheduleEntry = import.Schedule[i];
            var path = $"schedule[{i}]";

            if (scheduleEntry.Date < import.StartDate || scheduleEntry.Date > import.EndDate)
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "error",
                    Category = "validation",
                    Path = $"{path}.date",
                    Message = $"Schedule date {scheduleEntry.Date} is outside plan range ({import.StartDate} to {import.EndDate})",
                    Resolution = "Ensure all schedule dates fall within the plan date range"
                });
            }

            if (!scheduleEntry.Meals.Any())
            {
                issues.Add(new ValidationIssueDto
                {
                    Severity = "warning",
                    Category = "validation",
                    Path = $"{path}.meals",
                    Message = $"Schedule entry for {scheduleEntry.Date} has no meals",
                    Resolution = "Add at least one meal or remove the schedule entry"
                });
            }

            for (int j = 0; j < scheduleEntry.Meals.Count; j++)
            {
                var meal = scheduleEntry.Meals[j];
                var mealPath = $"{path}.meals[{j}]";

                if (!MealType.All.Contains(meal.Type.ToLowerInvariant()))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "validation",
                        Path = $"{mealPath}.type",
                        Message = $"Invalid meal type '{meal.Type}'",
                        Resolution = $"Use one of: {string.Join(", ", MealType.All)}"
                    });
                }

                if (meal.Servings <= 0)
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "validation",
                        Path = $"{mealPath}.servings",
                        Item = meal.Recipe,
                        Message = "Servings must be positive",
                        Resolution = "Provide a valid number of servings"
                    });
                }
            }
        }
    }

    private void ValidateReferentialIntegrity(ImportDto import, List<ValidationIssueDto> issues)
    {
        // Placeholder: cross-references are validated in ValidateConflictsAsync
    }

    private void ValidateBusinessRules(ImportDto import, List<ValidationIssueDto> issues)
    {
        var duplicateProducts = import.Products
            .GroupBy(p => p.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.First().Name)
            .ToList();

        foreach (var productName in duplicateProducts)
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "error",
                Category = "validation",
                Path = "products",
                Item = productName,
                Message = $"Duplicate product '{productName}' found in import",
                Resolution = "Remove duplicate products from the import"
            });
        }

        var duplicateRecipes = import.Recipes
            .GroupBy(r => r.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.First().Name)
            .ToList();

        foreach (var recipeName in duplicateRecipes)
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "error",
                Category = "validation",
                Path = "recipes",
                Item = recipeName,
                Message = $"Duplicate recipe '{recipeName}' found in import",
                Resolution = "Remove duplicate recipes from the import"
            });
        }

        var dayCount = (import.EndDate.ToDateTime(TimeOnly.MinValue) - import.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
        var mealCount = import.Schedule.Sum(s => s.Meals.Count);

        if (dayCount > 365)
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "warning",
                Category = "performance",
                Path = "schedule",
                Message = $"Large import detected: {dayCount} days",
                Resolution = "This may take longer to process"
            });
        }

        if (mealCount > 1000)
        {
            issues.Add(new ValidationIssueDto
            {
                Severity = "warning",
                Category = "performance",
                Path = "schedule",
                Message = $"Large import detected: {mealCount} meal entries",
                Resolution = "This may take 10-15 seconds to complete"
            });
        }
    }

    private async Task ValidateConflictsAsync(
        ImportDto import,
        string userId,
        List<ValidationIssueDto> issues,
        ImportPlanDto plan)
    {
        // Collect all product names referenced anywhere (import products + recipe ingredients)
        var importProductNames = import.Products.Select(p => p.Name.ToLowerInvariant()).ToHashSet();
        var ingredientProductNames = import.Recipes
            .SelectMany(r => r.Ingredients.Select(i => i.Product.ToLowerInvariant()))
            .ToHashSet();
        var allReferencedProductNames = importProductNames.Union(ingredientProductNames).ToList();

        // Single query covers both conflict detection and referential integrity for products
        var existingProducts = await _db.Products
            .IgnoreQueryFilters()
            .Where(p => allReferencedProductNames.Contains(p.Name.ToLower()))
            .AsNoTracking()
            .ToListAsync();

        var existingProductNameSet = existingProducts
            .Select(p => p.Name.ToLower())
            .ToHashSet();

        // Check product conflicts
        foreach (var importProduct in import.Products)
        {
            var existing = existingProducts
                .FirstOrDefault(p => p.Name.Equals(importProduct.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (existing.CreatedByUserId == userId)
                {
                    plan.ProductsToUpdate++;
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "info",
                        Category = "conflict",
                        Path = "products",
                        Item = importProduct.Name,
                        Message = $"Product '{importProduct.Name}' will be updated (you own it)",
                        Resolution = "The existing product will be updated with new values"
                    });
                }
                else
                {
                    plan.ProductsToReuse++;
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "conflict",
                        Path = "products",
                        Item = importProduct.Name,
                        Message = $"Product '{importProduct.Name}' already exists and is owned by another user",
                        Resolution = "Rename your product to something unique OR remove from products array to use existing",
                        ExistingItem = new
                        {
                            existing.Id,
                            OwnedBy = "another user",
                            existing.CaloriesPer100g,
                            existing.ProteinPer100g
                        }
                    });
                }
            }
            else
            {
                plan.ProductsToCreate++;
            }
        }

        // Referential integrity: check recipe ingredients reference known products
        var allAvailableProductNames = importProductNames.Union(existingProductNameSet).ToHashSet();
        foreach (var recipe in import.Recipes)
        {
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!allAvailableProductNames.Contains(ingredient.Product.ToLowerInvariant()))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "referential",
                        Path = "recipes",
                        Item = recipe.Name,
                        Message = $"Recipe '{recipe.Name}' references unknown product '{ingredient.Product}'",
                        Resolution = "Add the product to the products array or ensure it exists in the database"
                    });
                }
            }
        }

        // Collect all recipe names referenced anywhere
        var importRecipeNames = import.Recipes.Select(r => r.Name.ToLowerInvariant()).ToHashSet();
        var scheduledRecipeNames = import.Schedule
            .SelectMany(s => s.Meals.Select(m => m.Recipe.ToLowerInvariant()))
            .ToHashSet();
        var allReferencedRecipeNames = importRecipeNames.Union(scheduledRecipeNames).ToList();

        // Single query covers both conflict detection and referential integrity for recipes
        var existingRecipes = await _db.Recipes
            .IgnoreQueryFilters()
            .Where(r => allReferencedRecipeNames.Contains(r.Name.ToLower()))
            .AsNoTracking()
            .ToListAsync();

        var existingRecipeNameSet = existingRecipes
            .Select(r => r.Name.ToLower())
            .ToHashSet();

        // Check recipe conflicts
        foreach (var importRecipe in import.Recipes)
        {
            var existing = existingRecipes
                .FirstOrDefault(r => r.Name.Equals(importRecipe.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (existing.CreatedByUserId == userId)
                {
                    plan.RecipesToUpdate++;
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "info",
                        Category = "conflict",
                        Path = "recipes",
                        Item = importRecipe.Name,
                        Message = $"Recipe '{importRecipe.Name}' will be updated (you own it)",
                        Resolution = "The existing recipe will be updated with new ingredients"
                    });
                }
                else
                {
                    plan.RecipesToReuse++;
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "conflict",
                        Path = "recipes",
                        Item = importRecipe.Name,
                        Message = $"Recipe '{importRecipe.Name}' already exists and is owned by another user",
                        Resolution = "Rename your recipe to something unique OR remove from recipes array to use existing",
                        ExistingItem = new
                        {
                            existing.Id,
                            OwnedBy = "another user",
                            existing.Servings
                        }
                    });
                }
            }
            else
            {
                plan.RecipesToCreate++;
            }
        }

        // Referential integrity: check scheduled meals reference known recipes
        var allAvailableRecipeNames = importRecipeNames.Union(existingRecipeNameSet).ToHashSet();
        foreach (var scheduleEntry in import.Schedule)
        {
            foreach (var meal in scheduleEntry.Meals)
            {
                if (!allAvailableRecipeNames.Contains(meal.Recipe.ToLowerInvariant()))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Severity = "error",
                        Category = "referential",
                        Path = "schedule",
                        Item = $"{scheduleEntry.Date} - {meal.Type}",
                        Message = $"Meal references unknown recipe '{meal.Recipe}'",
                        Resolution = "Add the recipe to the recipes array or ensure it exists in the database"
                    });
                }
            }
        }
    }

    private void CalculatePlanStatistics(ImportDto import, ImportPlanDto plan)
    {
        plan.MealEntriesToCreate = import.Schedule.Sum(s => s.Meals.Count);

        var estimatedSeconds =
            (plan.ProductsToCreate + plan.ProductsToUpdate) * 0.01 +
            (plan.RecipesToCreate + plan.RecipesToUpdate) * 0.02 +
            plan.MealEntriesToCreate * 0.001;

        plan.EstimatedDurationSeconds = (int)Math.Ceiling(estimatedSeconds);
    }
}
