namespace DietPlanner.Application.Queries.GetMealEntries;

public sealed record MealEntryDto(
    Guid Id,
    DateOnly Date,
    string MealType,
    Guid RecipeId,
    string RecipeName,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder,
    DateTime CreatedAt);
