namespace DietPlanner.Mcp;

using System.ComponentModel;

/// <summary>
/// One slot of a meal schedule to save.
/// </summary>
/// <param name="Id">Id of an existing slot to keep, from <c>get_diet_context</c>; omit for a new slot.</param>
/// <param name="Name">Display name, e.g. Breakfast.</param>
/// <param name="DefaultTime">Usual time of the meal, HH:mm.</param>
public sealed record MealSlotEdit(
    [property: Description("Id of an existing slot to keep, from get_diet_context; omit for a new slot.")] Guid? Id,
    [property: Description("Display name, e.g. Breakfast.")] string Name,
    [property: Description("Usual time of the meal, HH:mm.")] string DefaultTime);
