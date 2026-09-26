namespace DietPlanner.Application.Queries.SearchRecipes;

/// <summary>
/// One ingredient line of a recipe with its product resolved.
/// </summary>
/// <param name="Id">Identifier of the line.</param>
/// <param name="ProductId">The product.</param>
/// <param name="ProductName">Display name of the product.</param>
/// <param name="Amount">Quantity in <paramref name="Unit"/> for the full recipe.</param>
/// <param name="Unit">Unit of the amount.</param>
public sealed record RecipeIngredientDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Amount,
    string Unit);

/// <summary>
/// Absolute macro totals.
/// </summary>
/// <param name="Calories">Energy in kcal.</param>
/// <param name="Protein">Protein in grams.</param>
/// <param name="Carbs">Carbohydrates in grams.</param>
/// <param name="Fat">Fat in grams.</param>
/// <param name="Fiber">Fibre in grams.</param>
public sealed record NutritionDto(
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber);

/// <summary>
/// A recipe with its ingredients and nutrition calculated from their products.
/// </summary>
/// <param name="Id">Identifier of the recipe.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Instructions">Optional preparation steps.</param>
/// <param name="Servings">Portions the ingredient amounts yield.</param>
/// <param name="PrepTimeMinutes">Optional preparation time.</param>
/// <param name="CreatedByUserId">Auth subject of the creator.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC; <see langword="null"/> if never changed.</param>
/// <param name="IsOwner">Whether the caller created it.</param>
/// <param name="Visibility">Who besides the creator can see it: <c>Private</c>, <c>Household</c> or <c>Public</c>.</param>
/// <param name="CanEdit">Whether the caller may edit or delete it (creator, or an adult of its household).</param>
/// <param name="Ingredients">The ingredient lines.</param>
/// <param name="NutritionPerServing">Macros for one serving, when calculated.</param>
/// <param name="TotalNutrition">Macros for the whole recipe, when calculated.</param>
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
    string Visibility,
    bool CanEdit,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    NutritionDto? NutritionPerServing = null,
    NutritionDto? TotalNutrition = null);
