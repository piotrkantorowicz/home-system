namespace DietPlanner.Application.Queries.SearchRecipes;

public sealed record RecipeIngredientDto(
    Guid Id,
    Guid ProductId,
    decimal Amount,
    string Unit);

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
    IReadOnlyList<RecipeIngredientDto> Ingredients);
