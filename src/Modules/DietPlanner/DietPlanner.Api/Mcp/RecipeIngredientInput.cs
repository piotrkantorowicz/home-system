namespace DietPlanner.Api.Mcp;

using System.ComponentModel;

/// <summary>
/// One ingredient line of a recipe to create.
/// </summary>
/// <param name="ProductId">A product the caller can see, from <c>search_products</c> or <c>create_product</c>.</param>
/// <param name="Amount">Quantity for the whole recipe (all servings); must be positive.</param>
/// <param name="Unit">Unit of the amount, e.g. g, ml, piece, tbsp, cup.</param>
public sealed record RecipeIngredientInput(
    [property: Description("A product the caller can see, from search_products or create_product.")] Guid ProductId,
    [property: Description("Quantity for the whole recipe (all servings); must be positive.")] decimal Amount,
    [property: Description("Unit of the amount, e.g. g, ml, piece, tbsp, cup.")] string Unit);
