namespace DietPlanner.Api.Mcp;

/// <summary>
/// The product a <c>create_product</c> call created, or created on an earlier call with the same operation key.
/// </summary>
/// <param name="ProductId">Id of the product, for recipe ingredients.</param>
/// <param name="Name">Its name.</param>
/// <param name="Visibility"><c>Private</c> or <c>Household</c>.</param>
public sealed record CreatedProductDto(Guid ProductId, string Name, string Visibility);
