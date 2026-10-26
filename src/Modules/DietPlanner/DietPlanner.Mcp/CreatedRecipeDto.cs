namespace DietPlanner.Mcp;

/// <summary>
/// The recipe a <c>create_recipe</c> call created, or created on an earlier call with the same operation key.
/// </summary>
/// <param name="RecipeId">Id of the recipe.</param>
/// <param name="Name">Its name; also the import reuse key.</param>
/// <param name="Visibility"><c>Private</c> or <c>Household</c>.</param>
public sealed record CreatedRecipeDto(Guid RecipeId, string Name, string Visibility);
