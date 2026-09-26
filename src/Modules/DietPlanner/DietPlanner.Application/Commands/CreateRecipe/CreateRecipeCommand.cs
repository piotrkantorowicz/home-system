namespace DietPlanner.Application.Commands.CreateRecipe;

using Shared.Abstractions.Cqrs;

/// <summary>
/// One ingredient line supplied when creating or updating a recipe.
/// </summary>
/// <param name="ProductId">The product used; must exist.</param>
/// <param name="Amount">Quantity for the full recipe; positive.</param>
/// <param name="Unit">Unit of the amount, e.g. <c>g</c>, <c>ml</c>, <c>cup</c>, <c>piece</c>.</param>
public sealed record CreateRecipeIngredientRequest(
    Guid ProductId,
    decimal Amount,
    string Unit);

/// <summary>
/// Creates a recipe with its ingredients, owned by the caller, and returns its id.
/// </summary>
/// <param name="Name">Display name; required and unique per user.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Instructions">Optional preparation steps.</param>
/// <param name="Servings">Portions the ingredient amounts yield; positive.</param>
/// <param name="PrepTimeMinutes">Optional preparation time.</param>
/// <param name="Ingredients">The complete ingredient list; on update it replaces the existing lines.</param>
/// <param name="Visibility"><c>Private</c>, <c>Household</c> or <c>Public</c>; <see langword="null"/> means <c>Household</c>.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record CreateRecipeCommand(
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<CreateRecipeIngredientRequest> Ingredients,
    string? Visibility,
    string UserId) : ICommand<Guid>;
