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
    DateTime CreatedAt,
    string Status,
    ActualRecipeDto? ActualRecipe,
    IReadOnlyList<ActualProductDto> ActualProducts,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber);

public sealed record ActualRecipeDto(Guid Id, string Name);

public sealed record ActualProductDto(
    Guid Id, Guid ProductId, string ProductName, decimal Amount, string Unit);
