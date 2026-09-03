namespace DietPlanner.Application.Queries.GetShoppingList;

public sealed record ShoppingListItemDto(
    Guid ProductId,
    string ProductName,
    decimal TotalAmount,
    string Unit);
