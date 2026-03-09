using System.ComponentModel;

namespace DietPlanner.Api.Features.DietPlans.Import;

/// <summary>
/// Main import DTO representing the entire diet plan JSON structure.
/// </summary>
public class ImportDto
{
    [Description("Name for the diet plan (max 200 chars)")]
    public required string PlanName { get; set; }

    [Description("Plan start date (YYYY-MM-DD)")]
    public required DateOnly StartDate { get; set; }

    [Description("Plan end date (YYYY-MM-DD, must be >= startDate)")]
    public required DateOnly EndDate { get; set; }

    [Description("Products to create or update during import")]
    public List<ImportProductDto> Products { get; set; } = new();

    [Description("Recipes to create or update during import")]
    public List<ImportRecipeDto> Recipes { get; set; } = new();

    [Description("Daily meal schedule entries (one per day within the date range)")]
    public List<ImportScheduleDto> Schedule { get; set; } = new();
}

/// <summary>
/// Product definition in import JSON.
/// </summary>
public class ImportProductDto
{
    [Description("Product name (must be unique)")]
    public required string Name { get; set; }

    [Description("Calories per 100g (0-9000)")]
    public decimal? CaloriesPer100g { get; set; }

    [Description("Protein per 100g in grams (0-100)")]
    public decimal? ProteinPer100g { get; set; }

    [Description("Carbohydrates per 100g in grams (0-100)")]
    public decimal? CarbsPer100g { get; set; }

    [Description("Fat per 100g in grams (0-100)")]
    public decimal? FatPer100g { get; set; }

    [Description("Measurement unit: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece")]
    public string Unit { get; set; } = "g";

    [Description("Density in g/ml (required for volume unit conversion, defaults to water 1.0)")]
    public decimal? DensityGramsPerMl { get; set; }

    [Description("Weight of one piece in grams (required when unit is 'piece')")]
    public decimal? GramPerPiece { get; set; }
}

/// <summary>
/// Recipe definition in import JSON.
/// </summary>
public class ImportRecipeDto
{
    [Description("Recipe name")]
    public required string Name { get; set; }

    [Description("Short description of the recipe")]
    public string? Description { get; set; }

    [Description("Step-by-step cooking instructions")]
    public string? Instructions { get; set; }

    [Description("Number of servings (default: 1)")]
    public int Servings { get; set; } = 1;

    [Description("Preparation time in minutes")]
    public int? PrepTimeMinutes { get; set; }

    [Description("List of ingredients referencing products by name")]
    public List<ImportIngredientDto> Ingredients { get; set; } = new();
}

/// <summary>
/// Ingredient within a recipe in import JSON.
/// </summary>
public class ImportIngredientDto
{
    [Description("Product name (must match a product in the products array or exist in the database)")]
    public required string Product { get; set; }

    [Description("Amount of the product")]
    public decimal Amount { get; set; }

    [Description("Measurement unit: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece")]
    public required string Unit { get; set; }
}

/// <summary>
/// Daily schedule entry in import JSON.
/// </summary>
public class ImportScheduleDto
{
    [Description("Date for this schedule entry (must be within startDate-endDate range)")]
    public required DateOnly Date { get; set; }

    [Description("List of meals for this day")]
    public List<ImportMealDto> Meals { get; set; } = new();
}

/// <summary>
/// Individual meal within a schedule entry.
/// </summary>
public class ImportMealDto
{
    [Description("Meal type: breakfast, lunch, dinner, snack")]
    public required string Type { get; set; }

    [Description("Recipe name (must match a recipe in the recipes array or exist in the database)")]
    public required string Recipe { get; set; }

    [Description("Number of servings (default: 1)")]
    public decimal Servings { get; set; } = 1m;

    [Description("Optional notes for this meal")]
    public string? Notes { get; set; }
}

/// <summary>
/// Validation result returned from the validate endpoint.
/// </summary>
public class ValidationResultDto
{
    [Description("Whether the import JSON passed all validation checks")]
    public bool Valid { get; set; }

    [Description("Whether the import can proceed (no blocking errors)")]
    public bool CanProceed { get; set; }

    [Description("Summary counts of errors, warnings, and info messages")]
    public ValidationSummaryDto Summary { get; set; } = new();

    [Description("Detailed list of validation issues")]
    public List<ValidationIssueDto> Issues { get; set; } = new();

    [Description("Execution plan showing what will be created/updated")]
    public ImportPlanDto? Plan { get; set; }
}

/// <summary>
/// Summary of validation issues.
/// </summary>
public class ValidationSummaryDto
{
    [Description("Number of blocking errors")]
    public int Errors { get; set; }

    [Description("Number of non-blocking warnings")]
    public int Warnings { get; set; }

    [Description("Number of informational messages")]
    public int Info { get; set; }
}

/// <summary>
/// Individual validation issue.
/// </summary>
public class ValidationIssueDto
{
    [Description("Severity level: error, warning, info")]
    public required string Severity { get; set; }

    [Description("Issue category: schema, validation, conflict, referential, performance")]
    public required string Category { get; set; }

    [Description("JSON path to the problematic field, e.g. 'products[0].name'")]
    public string? Path { get; set; }

    [Description("Name of the affected item, e.g. 'Chicken Breast'")]
    public string? Item { get; set; }

    [Description("Human-readable description of the issue")]
    public required string Message { get; set; }

    [Description("Suggested resolution for the issue")]
    public string? Resolution { get; set; }

    [Description("Details about an existing conflicting item (if applicable)")]
    public object? ExistingItem { get; set; }
}

/// <summary>
/// Execution plan showing what will be created/updated during import.
/// </summary>
public class ImportPlanDto
{
    [Description("Number of new products to be created")]
    public int ProductsToCreate { get; set; }

    [Description("Number of existing products to be updated")]
    public int ProductsToUpdate { get; set; }

    [Description("Number of existing products to be reused as-is")]
    public int ProductsToReuse { get; set; }

    [Description("Number of new recipes to be created")]
    public int RecipesToCreate { get; set; }

    [Description("Number of existing recipes to be updated")]
    public int RecipesToUpdate { get; set; }

    [Description("Number of existing recipes to be reused as-is")]
    public int RecipesToReuse { get; set; }

    [Description("Total number of meal entries to be created")]
    public int MealEntriesToCreate { get; set; }

    [Description("Date range of the plan (e.g. '2025-06-01 to 2025-06-30')")]
    public string DateRange { get; set; } = string.Empty;

    [Description("Estimated import duration in seconds")]
    public int EstimatedDurationSeconds { get; set; }
}

/// <summary>
/// Result returned from successful import execution.
/// </summary>
public class ImportResultDto
{
    [Description("ID of the created diet plan")]
    public Guid DietPlanId { get; set; }

    [Description("Success message")]
    public required string Message { get; set; }

    [Description("Statistics about what was created/updated during import")]
    public ImportStatsDto Stats { get; set; } = new();
}

/// <summary>
/// Statistics about completed import.
/// </summary>
public class ImportStatsDto
{
    [Description("Number of products created")]
    public int ProductsCreated { get; set; }

    [Description("Number of products updated")]
    public int ProductsUpdated { get; set; }

    [Description("Number of products reused")]
    public int ProductsReused { get; set; }

    [Description("Number of recipes created")]
    public int RecipesCreated { get; set; }

    [Description("Number of recipes updated")]
    public int RecipesUpdated { get; set; }

    [Description("Number of recipes reused")]
    public int RecipesReused { get; set; }

    [Description("Number of meal entries created")]
    public int MealEntriesCreated { get; set; }

    [Description("Import duration in seconds")]
    public double DurationSeconds { get; set; }
}
