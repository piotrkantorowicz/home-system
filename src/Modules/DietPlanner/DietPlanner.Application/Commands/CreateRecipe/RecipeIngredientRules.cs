namespace DietPlanner.Application.Commands.CreateRecipe;

using DietPlanner.Domain.Services;
using Shared.Abstractions.Cqrs;

/// <summary>Ingredient-line rules shared by create and update, so a recipe cannot be edited into a state create rejects.</summary>
internal static class RecipeIngredientRules
{
    public const int MaxIngredients = 100;

    public static IEnumerable<ValidationError> Validate(IReadOnlyList<CreateRecipeIngredientRequest> ingredients)
    {
        if (ingredients.Count > MaxIngredients)
            yield return new ValidationError("Ingredients", $"Recipe must not have more than {MaxIngredients} ingredients.");

        for (var i = 0; i < ingredients.Count; i++)
        {
            var ingredient = ingredients[i];
            if (ingredient.Amount <= 0)
                yield return new ValidationError($"Ingredients[{i}].Amount", "Ingredient amount must be positive.");
            if (string.IsNullOrWhiteSpace(ingredient.Unit) || !UnitConverter.IsValidUnit(ingredient.Unit))
                yield return new ValidationError($"Ingredients[{i}].Unit", $"Unit must be one of: {string.Join(", ", UnitConverter.GetSupportedUnits())}.");
        }
    }
}
