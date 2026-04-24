namespace DietPlanner.Application.Queries.GetMealEntries;

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
    DateTime CreatedAt);
