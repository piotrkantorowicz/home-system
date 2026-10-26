namespace DietPlanner.Mcp;

/// <summary>
/// A meal's eaten state after a consumption tool ran; get_meal_entries shows its nutrition.
/// </summary>
/// <param name="MealId">Meal entry id.</param>
/// <param name="Status"><c>Planned</c>, <c>Done</c> (eaten as planned) or <c>Modified</c> (eaten with changes).</param>
public sealed record MealStatusDto(Guid MealId, string Status);
