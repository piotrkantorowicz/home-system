namespace DietPlanner.Mcp;

/// <summary>
/// Confirms a delete tool removed an item.
/// </summary>
/// <param name="Id">Id of the deleted item.</param>
public sealed record DeletedItemDto(Guid Id);
