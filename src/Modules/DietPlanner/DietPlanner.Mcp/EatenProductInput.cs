namespace DietPlanner.Mcp;

using System.ComponentModel;

/// <summary>
/// One product actually eaten, in a meal override.
/// </summary>
/// <param name="ProductId">A product the caller can see, from <c>search_products</c> or <c>create_product</c>.</param>
/// <param name="Amount">Quantity eaten; must be positive.</param>
/// <param name="Unit">Unit of the amount, e.g. g, ml, piece, tbsp, cup.</param>
public sealed record EatenProductInput(
    [property: Description("A product the caller can see, from search_products or create_product.")] Guid ProductId,
    [property: Description("Quantity eaten; must be positive.")] decimal Amount,
    [property: Description("Unit of the amount, e.g. g, ml, piece, tbsp, cup.")] string Unit);
