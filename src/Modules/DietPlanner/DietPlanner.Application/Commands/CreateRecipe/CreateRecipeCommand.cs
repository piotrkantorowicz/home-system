namespace DietPlanner.Application.Commands.CreateRecipe;

using Shared.Abstractions.CQRS;

public sealed record CreateRecipeIngredientRequest(
    Guid ProductId,
    decimal Amount,
    string Unit);

public sealed record CreateRecipeCommand(
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<CreateRecipeIngredientRequest> Ingredients,
    string UserId) : ICommand<Guid>;
