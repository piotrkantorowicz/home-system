namespace DietPlanner.Mcp;

/// <summary>
/// The shopping-list range whose ticks <c>clear_shopping_checks</c> removed.
/// </summary>
/// <param name="From">First day of the range.</param>
/// <param name="To">Last day of the range.</param>
public sealed record ClearedShoppingChecksDto(DateOnly From, DateOnly To);
