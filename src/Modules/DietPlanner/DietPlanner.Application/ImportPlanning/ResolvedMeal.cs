namespace DietPlanner.Application.ImportPlanning;

using DietPlanner.Domain.ValueObjects;

/// <summary>
/// One planned meal of an import after resolution.
/// </summary>
/// <param name="Date">Calendar day of the meal.</param>
/// <param name="SlotName">Name of the schedule slot the meal lands in; the slot may be created by the import in legacy mode.</param>
/// <param name="RecipeId">Existing or batch-created recipe.</param>
/// <param name="RecipeName">The recipe's own name (the file's definition or the catalog's spelling).</param>
/// <param name="Servings">Servings to plan.</param>
/// <param name="Notes">Optional free-text note.</param>
internal sealed record ResolvedMeal(DateOnly Date, string SlotName, RecipeId RecipeId, string RecipeName, decimal Servings, string? Notes);
