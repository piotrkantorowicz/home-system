namespace DietPlanner.Application.ImportPlanning;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Domain.Aggregates;

/// <summary>
/// The authoritative outcome of resolving an import file, shared by validation (preview) and execution.
/// When <see cref="HasErrors"/> is false every product, recipe and meal is resolved and nothing is skipped.
/// </summary>
/// <param name="Issues">Every finding, in file order.</param>
/// <param name="Products">Distinct products of the file, reused or new.</param>
/// <param name="Recipes">Distinct recipes of the file, reused or new.</param>
/// <param name="Meals">Every planned meal of the file.</param>
/// <param name="Schedule">The person's tracked meal schedule; <see langword="null"/> when execution must create the default one (legacy mode only).</param>
/// <param name="CreatesOtherSlot">True when execution appends an "Other" slot for unknown meal types (legacy mode only).</param>
/// <param name="CatalogRecipes">Catalog recipes the file's names matched, as loaded; lets a preview bind to their current content.</param>
internal sealed record ResolvedImport(
    IReadOnlyList<ValidationIssueDto> Issues,
    IReadOnlyList<ResolvedProduct> Products,
    IReadOnlyList<ResolvedRecipe> Recipes,
    IReadOnlyList<ResolvedMeal> Meals,
    MealScheduleConfig? Schedule,
    bool CreatesOtherSlot,
    IReadOnlyList<Recipe> CatalogRecipes)
{
    public bool HasErrors => Issues.Any(i => i.Severity == ImportPlanResolver.Error);
}
