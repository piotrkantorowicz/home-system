namespace DietPlanner.Application.Commands.UpdateRecipe;

using DietPlanner.Application.Commands.CreateRecipe;
using Shared.Abstractions.CQRS;

public sealed record UpdateRecipeCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<CreateRecipeIngredientRequest> Ingredients,
    string UserId) : ICommand;
