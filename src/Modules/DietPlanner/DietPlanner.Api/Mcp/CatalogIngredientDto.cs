namespace DietPlanner.Api.Mcp;

/// <summary>
/// One ingredient line of a recipe.
/// </summary>
/// <param name="ProductId">The product used.</param>
/// <param name="ProductName">Its name.</param>
/// <param name="Amount">Quantity for the whole recipe.</param>
/// <param name="Unit">Unit of <paramref name="Amount"/>.</param>
public sealed record CatalogIngredientDto(Guid ProductId, string ProductName, decimal Amount, string Unit);
