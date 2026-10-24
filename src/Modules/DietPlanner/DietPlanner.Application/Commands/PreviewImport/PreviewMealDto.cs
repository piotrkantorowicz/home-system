namespace DietPlanner.Application.Commands.PreviewImport;

/// <summary>
/// One meal an import plans.
/// </summary>
/// <param name="Slot">Meal slot name.</param>
/// <param name="Recipe">Recipe name.</param>
/// <param name="Servings">Servings planned.</param>
/// <param name="NewRecipe">True when the recipe is created by the import.</param>
public sealed record PreviewMealDto(string Slot, string Recipe, decimal Servings, bool NewRecipe);
