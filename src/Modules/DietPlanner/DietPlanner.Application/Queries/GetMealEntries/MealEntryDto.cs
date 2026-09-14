namespace DietPlanner.Application.Queries.GetMealEntries;

/// <summary>
/// A planned meal with its slot and recipe resolved, its completion state, and the macros it contributes — from the planned recipe, or from the override when the entry is modified.
/// </summary>
/// <param name="Id">Identifier of the entry.</param>
/// <param name="Date">Calendar day of the meal.</param>
/// <param name="MealSlotId">Slot of the user's schedule.</param>
/// <param name="MealSlotName">Display name of the slot.</param>
/// <param name="MealSlotDefaultTime">The slot's default time of day.</param>
/// <param name="MealSlotSortOrder">The slot's position in the schedule.</param>
/// <param name="RecipeId">The planned recipe.</param>
/// <param name="RecipeName">Display name of the planned recipe.</param>
/// <param name="Servings">Servings of the planned recipe.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="MealTime">Time overriding the slot default, if set.</param>
/// <param name="SequenceOrder">Ordering among entries in the same slot, if set.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="Status"><c>Planned</c>, <c>Done</c> or <c>Modified</c>.</param>
/// <param name="ActualRecipe">Replacement recipe recorded by an override, if any.</param>
/// <param name="ActualProducts">Products recorded by an override; empty unless modified.</param>
/// <param name="Calories">Energy this entry contributes, kcal.</param>
/// <param name="Protein">Protein this entry contributes, grams.</param>
/// <param name="Carbs">Carbohydrates this entry contributes, grams.</param>
/// <param name="Fat">Fat this entry contributes, grams.</param>
/// <param name="Fiber">Fibre this entry contributes, grams.</param>
public sealed record MealEntryDto(
    Guid Id,
    DateOnly Date,
    Guid MealSlotId,
    string MealSlotName,
    TimeOnly MealSlotDefaultTime,
    int MealSlotSortOrder,
    Guid RecipeId,
    string RecipeName,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder,
    DateTime CreatedAt,
    string Status,
    ActualRecipeDto? ActualRecipe,
    IReadOnlyList<ActualProductDto> ActualProducts,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber);

/// <summary>
/// The recipe eaten instead of the planned one.
/// </summary>
/// <param name="Id">Identifier of the recipe.</param>
/// <param name="Name">Display name.</param>
public sealed record ActualRecipeDto(Guid Id, string Name);

/// <summary>
/// A product actually eaten as part of a modified meal.
/// </summary>
/// <param name="Id">Identifier of the override line.</param>
/// <param name="ProductId">The product.</param>
/// <param name="ProductName">Display name of the product.</param>
/// <param name="Amount">Quantity in <paramref name="Unit"/>.</param>
/// <param name="Unit">Unit of the amount.</param>
public sealed record ActualProductDto(
    Guid Id, Guid ProductId, string ProductName, decimal Amount, string Unit);
