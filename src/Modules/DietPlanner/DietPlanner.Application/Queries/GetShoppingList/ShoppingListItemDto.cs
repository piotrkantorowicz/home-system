namespace DietPlanner.Application.Queries.GetShoppingList;

/// <summary>
/// Total amount of one product needed for the planned meals in a range; one line per product and unit, sorted by product name.
/// </summary>
/// <param name="ProductId">The product.</param>
/// <param name="ProductName">Display name of the product.</param>
/// <param name="TotalAmount">Summed quantity in <paramref name="Unit"/> across all planned meals.</param>
/// <param name="Unit">Unit the ingredients were specified in.</param>
public sealed record ShoppingListItemDto(
    Guid ProductId,
    string ProductName,
    decimal TotalAmount,
    string Unit);
