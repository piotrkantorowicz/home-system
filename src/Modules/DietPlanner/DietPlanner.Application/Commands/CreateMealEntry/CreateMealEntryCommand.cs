namespace DietPlanner.Application.Commands.CreateMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Plans a meal for the caller or another household member and returns the new entry's id.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="Date">Calendar day of the meal.</param>
/// <param name="MealSlotId">Slot of the planned-for person's meal schedule.</param>
/// <param name="RecipeId">Recipe to plan; must exist and be visible to the caller.</param>
/// <param name="Servings">Servings of the recipe; positive.</param>
/// <param name="Notes">Optional free-text note.</param>
/// <param name="MealTime">Optional time overriding the slot's default.</param>
/// <param name="SequenceOrder">Optional ordering among entries in the same slot.</param>
/// <param name="AuthSubject">Auth subject of the caller; resolves their household.</param>
/// <param name="ForPersonId">Household member the meal is for; the caller when <see langword="null"/>.</param>
public sealed record CreateMealEntryCommand(
    Guid PersonId,
    DateOnly Date,
    Guid MealSlotId,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder,
    string AuthSubject,
    Guid? ForPersonId = null) : ICommand<Guid>;
