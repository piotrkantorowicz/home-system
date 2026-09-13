namespace DietPlanner.Application.Queries.GetShoppingList;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Sums the ingredients of every planned meal in a date range into one line per product. Includes the meals of everyone in the caller's household when the Household module can resolve it; falls back to the caller's own meals otherwise.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
/// <param name="From">First day to include, or <see langword="null"/> for no lower bound.</param>
/// <param name="To">Last day to include, or <see langword="null"/> for no upper bound.</param>
public sealed record GetShoppingListQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<ShoppingListItemDto>>;
