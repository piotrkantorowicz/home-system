namespace DietPlanner.Application.Commands.UpdateRecipe;

using DietPlanner.Application.Commands.CreateRecipe;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Replaces a recipe's header fields and its whole ingredient list.
/// </summary>
/// <param name="Id">Identifier of the recipe; must be owned by the caller.</param>
/// <param name="Name">Display name; required and unique per user.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Instructions">Optional preparation steps.</param>
/// <param name="Servings">Portions the ingredient amounts yield; positive.</param>
/// <param name="PrepTimeMinutes">Optional preparation time.</param>
/// <param name="Ingredients">The complete ingredient list; on update it replaces the existing lines.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record UpdateRecipeCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<CreateRecipeIngredientRequest> Ingredients,
    string UserId) : ICommand;
