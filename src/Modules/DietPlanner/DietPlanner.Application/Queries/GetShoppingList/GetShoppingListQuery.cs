namespace DietPlanner.Application.Queries.GetShoppingList;

using Shared.Abstractions.Cqrs;

public sealed record GetShoppingListQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<ShoppingListItemDto>>;
