namespace DietPlanner.Application.Commands.CreateMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Plans a meal for the caller and returns the new entry's id.
/// </summary>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="Date">Calendar day of the meal.</param>
/// <param name="MealSlotId">Slot of the caller's meal schedule.</param>
/// <param name="RecipeId">Recipe to plan; must exist and be visible to the caller.</param>
/// <param name="Servings">Servings of the recipe; positive.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="MealTime">Optional time overriding the slot's default.</param>
/// <param name="SequenceOrder">Optional ordering among entries in the same slot.</param>
public sealed record CreateMealEntryCommand(
    Guid PersonId,
    DateOnly Date,
    Guid MealSlotId,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder) : ICommand<Guid>;
