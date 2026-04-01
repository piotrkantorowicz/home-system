namespace DietPlanner.Application.Queries.SearchRecipes;

public sealed record RecipeIngredientDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Amount,
    string Unit);

public sealed record NutritionDto(
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber);

public sealed record RecipeDto(
    Guid Id,
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    string CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsOwner,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    NutritionDto? NutritionPerServing = null,
    NutritionDto? TotalNutrition = null);
