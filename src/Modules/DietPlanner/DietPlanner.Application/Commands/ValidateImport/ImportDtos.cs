namespace DietPlanner.Application.Commands.ValidateImport;

/// <summary>
/// The JSON document a user uploads to bulk-import products, recipes and a meal plan. Every section is
/// optional; items are matched to existing data by name (case-insensitive) so re-importing reuses
/// rather than duplicates.
/// </summary>
/// <param name="Products">Products to create or reuse by name.</param>
/// <param name="Recipes">Recipes to create or reuse by name; their ingredients reference products by name.</param>
/// <param name="Schedule">Days of planned meals; each meal references a recipe by name.</param>
public sealed record ImportDto(
    IReadOnlyList<ImportProductDto>? Products,
    IReadOnlyList<ImportRecipeDto>? Recipes,
    IReadOnlyList<ImportScheduleDto>? Schedule);

/// <summary>
/// One product in an import file; nutrition is per 100 g as on a label.
/// </summary>
/// <param name="Name">Product name; required and used as the reuse key.</param>
/// <param name="CaloriesPer100g">Energy per 100 g in kcal, if known.</param>
/// <param name="ProteinPer100g">Protein per 100 g in grams, if known.</param>
/// <param name="CarbsPer100g">Carbohydrates per 100 g in grams, if known.</param>
/// <param name="FatPer100g">Fat per 100 g in grams, if known.</param>
/// <param name="FiberPer100g">Fibre per 100 g in grams, if known.</param>
/// <param name="Unit">Default unit (<c>g</c>, <c>ml</c>, <c>piece</c>); <c>g</c> when omitted.</param>
/// <param name="DensityGramsPerMl">Grams per millilitre, needed for volume units.</param>
/// <param name="GramPerPiece">Grams per piece, needed for the <c>piece</c> unit.</param>
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

/// <summary>
/// One recipe in an import file.
/// </summary>
/// <param name="Name">Recipe name; required and used as the reuse key.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Instructions">Optional preparation steps.</param>
/// <param name="Servings">Portions the ingredient amounts yield; 1 when omitted.</param>
/// <param name="PrepTimeMinutes">Optional preparation time.</param>
/// <param name="Ingredients">Ingredient lines; an ingredient whose product is neither in the file nor already owned is an error.</param>
public sealed record ImportRecipeDto(
    string Name,
    string? Description,
    string? Instructions,
    int? Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<ImportIngredientDto>? Ingredients);

/// <summary>
/// One ingredient line of an imported recipe.
/// </summary>
/// <param name="Product">Product name, resolved against the file's products and the user's existing ones.</param>
/// <param name="Amount">Quantity for the full recipe; must be positive.</param>
/// <param name="Unit">Unit of the amount, e.g. <c>g</c>, <c>ml</c>, <c>cup</c>, <c>piece</c>.</param>
public sealed record ImportIngredientDto(
    string Product,
    decimal? Amount,
    string Unit);

/// <summary>
/// One day of an imported meal plan.
/// </summary>
/// <param name="Date">Calendar day in ISO 8601 (<c>yyyy-MM-dd</c>).</param>
/// <param name="Meals">Meals planned for that day.</param>
public sealed record ImportScheduleDto(
    string Date,
    IReadOnlyList<ImportMealDto>? Meals);

/// <summary>
/// One planned meal in an imported schedule.
/// </summary>
/// <param name="Type">Meal type (<c>breakfast</c>, <c>lunch</c>, <c>dinner</c>, <c>snack</c>), mapped to a slot of the user's schedule; unknown types land in an "Other" slot.</param>
/// <param name="Recipe">Recipe name, resolved against the file's recipes and the user's existing ones.</param>
/// <param name="Servings">Servings to plan; 1 when omitted.</param>
/// <param name="Notes">Optional free-text note.</param>
public sealed record ImportMealDto(
    string Type,
    string Recipe,
    decimal? Servings,
    string? Notes);

// ─── Validation Result ───────────────────────────────────────────────────────

/// <summary>
/// Outcome of a dry-run import: everything that would go wrong, plus a plan of what would be created
/// or reused when there are no errors.
/// </summary>
/// <param name="Valid">True when no issue has severity <c>error</c>.</param>
/// <param name="CanProceed">True when the import may be executed; currently the same as <paramref name="Valid"/>.</param>
/// <param name="Summary">Issue counts by severity.</param>
/// <param name="Issues">Every problem found, in file order.</param>
/// <param name="Plan">What executing the import would do; <see langword="null"/> when it cannot proceed.</param>
public sealed record ValidationResultDto(
    bool Valid,
    bool CanProceed,
    ValidationSummaryDto Summary,
    IReadOnlyList<ValidationIssueDto> Issues,
    ImportPlanDto? Plan);

/// <summary>
/// Number of validation issues per severity.
/// </summary>
/// <param name="Errors">Issues that block the import.</param>
/// <param name="Warnings">Issues worth reviewing that do not block the import.</param>
/// <param name="Info">Informational notes, e.g. an existing product that will be reused.</param>
public sealed record ValidationSummaryDto(int Errors, int Warnings, int Info);

/// <summary>
/// One finding from import validation.
/// </summary>
/// <param name="Severity"><c>error</c>, <c>warning</c> or <c>info</c>.</param>
/// <param name="Category"><c>validation</c> (bad value), <c>referential</c> (unresolved name) or <c>conflict</c> (already exists).</param>
/// <param name="Path">JSON path of the offending value, e.g. <c>recipes[2].ingredients[0].product</c>.</param>
/// <param name="Item">Name of the item involved, when it has one.</param>
/// <param name="Message">What is wrong, phrased for the end user.</param>
/// <param name="Resolution">How to fix it, when there is an obvious fix.</param>
public sealed record ValidationIssueDto(
    string Severity,
    string Category,
    string? Path,
    string? Item,
    string Message,
    string? Resolution);

/// <summary>
/// Counts of what executing a validated import would do.
/// </summary>
/// <param name="ProductsToCreate">Products not yet owned by the user.</param>
/// <param name="ProductsToReuse">Products matched by name to existing ones.</param>
/// <param name="RecipesToCreate">Recipes not yet owned by the user.</param>
/// <param name="RecipesToReuse">Recipes matched by name to existing ones.</param>
/// <param name="MealEntriesToCreate">Planned meals across all scheduled days.</param>
public sealed record ImportPlanDto(
    int ProductsToCreate,
    int ProductsToReuse,
    int RecipesToCreate,
    int RecipesToReuse,
    int MealEntriesToCreate);

// ─── Import Result ───────────────────────────────────────────────────────────

/// <summary>
/// Outcome of an executed import.
/// </summary>
/// <param name="Message">Human-readable confirmation.</param>
/// <param name="Stats">What was actually created and reused.</param>
public sealed record ImportResultDto(string Message, ImportStatsDto Stats);

/// <summary>
/// Counts of what an executed import did.
/// </summary>
/// <param name="ProductsCreated">Products created.</param>
/// <param name="ProductsReused">Existing products matched by name.</param>
/// <param name="RecipesCreated">Recipes created.</param>
/// <param name="RecipesReused">Existing recipes matched by name.</param>
/// <param name="MealEntriesCreated">Meal entries created.</param>
public sealed record ImportStatsDto(
    int ProductsCreated,
    int ProductsReused,
    int RecipesCreated,
    int RecipesReused,
    int MealEntriesCreated);
