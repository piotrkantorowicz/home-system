namespace DietPlanner.Application.ImportPlanning;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// One recipe of an import after resolution: either an existing catalog recipe to reuse or a new one
/// with every ingredient resolved and its id already assigned.
/// </summary>
/// <param name="Id">Existing recipe id, or the id the new recipe will get.</param>
/// <param name="Definition">The import definition that wins for this name.</param>
/// <param name="IsNew">True when executing the import creates the recipe.</param>
/// <param name="Ingredients">Resolved ingredient lines; only meaningful when <paramref name="IsNew"/>.</param>
internal sealed record ResolvedRecipe(
    RecipeId Id,
    ImportRecipeDto Definition,
    bool IsNew,
    IReadOnlyList<ResolvedIngredient> Ingredients);
