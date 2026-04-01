namespace DietPlanner.Application.Commands.ValidateImport;

public sealed record ImportDto(
    IReadOnlyList<ImportProductDto>? Products,
    IReadOnlyList<ImportRecipeDto>? Recipes,
    IReadOnlyList<ImportScheduleDto>? Schedule);

public sealed record ImportProductDto(
    string Name,
    decimal? CaloriesPer100g,
    decimal? ProteinPer100g,
    decimal? CarbsPer100g,
    decimal? FatPer100g,
    decimal? FiberPer100g,
    string? Unit,
    decimal? DensityGramsPerMl,
    decimal? GramPerPiece);

public sealed record ImportRecipeDto(
    string Name,
    string? Description,
    string? Instructions,
    int? Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<ImportIngredientDto>? Ingredients);

public sealed record ImportIngredientDto(
    string Product,
    decimal? Amount,
    string Unit);

public sealed record ImportScheduleDto(
    string Date,
    IReadOnlyList<ImportMealDto>? Meals);

public sealed record ImportMealDto(
    string Type,
    string Recipe,
    decimal? Servings,
    string? Notes);

// ─── Validation Result ───────────────────────────────────────────────────────

public sealed record ValidationResultDto(
    bool Valid,
    bool CanProceed,
    ValidationSummaryDto Summary,
    IReadOnlyList<ValidationIssueDto> Issues,
    ImportPlanDto? Plan);

public sealed record ValidationSummaryDto(int Errors, int Warnings, int Info);

public sealed record ValidationIssueDto(
    string Severity,
    string Category,
    string? Path,
    string? Item,
    string Message,
    string? Resolution);

public sealed record ImportPlanDto(
    int ProductsToCreate,
    int ProductsToReuse,
    int RecipesToCreate,
    int RecipesToReuse,
    int MealEntriesToCreate);

// ─── Import Result ───────────────────────────────────────────────────────────

public sealed record ImportResultDto(string Message, ImportStatsDto Stats);

public sealed record ImportStatsDto(
    int ProductsCreated,
    int ProductsReused,
    int RecipesCreated,
    int RecipesReused,
    int MealEntriesCreated);
