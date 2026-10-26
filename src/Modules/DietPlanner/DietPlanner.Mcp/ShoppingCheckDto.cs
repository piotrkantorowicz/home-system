namespace DietPlanner.Mcp;

/// <summary>
/// A shopping-list row's tick after <c>check_shopping_item</c>.
/// </summary>
/// <param name="ProductId">Product of the row.</param>
/// <param name="Unit">Unit of the row.</param>
/// <param name="IsChecked">Whether the row is now ticked as bought.</param>
public sealed record ShoppingCheckDto(Guid ProductId, string Unit, bool IsChecked);
