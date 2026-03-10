using System.ComponentModel;
using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Features.Recipes;

/// <summary>
/// Ingredient reference within a recipe create/update request.
/// </summary>
public record CreateRecipeIngredientRequest(
    [property: Description("Name of an existing product")] string ProductName,
    [property: Description("Amount of the product")] decimal Amount,
    [property: Description("Measurement unit: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece")] string Unit
);

/// <summary>
/// Shared interface used by validators to avoid rule duplication between Create and Update.
/// </summary>
public interface IRecipeRequest
{
    string Name { get; }
    string? Description { get; }
    string? Instructions { get; }
    int Servings { get; }
    int? PrepTimeMinutes { get; }
    List<CreateRecipeIngredientRequest> Ingredients { get; }
}

/// <summary>
/// Request body for creating a new recipe.
/// </summary>
public record CreateRecipeRequest(
    [property: Description("Recipe name (max 200 chars)")] string Name,
    [property: Description("Short description of the recipe")] string? Description,
    [property: Description("Step-by-step cooking instructions")] string? Instructions,
    [property: Description("Number of servings this recipe yields (1-100)")] int Servings,
    [property: Description("Estimated preparation time in minutes")] int? PrepTimeMinutes,
    [property: Description("List of ingredients with product references")] List<CreateRecipeIngredientRequest> Ingredients
) : IRecipeRequest;

/// <summary>
/// Request body for updating an existing recipe.
/// </summary>
public record UpdateRecipeRequest(
    [property: Description("Recipe name (max 200 chars)")] string Name,
    [property: Description("Short description of the recipe")] string? Description,
    [property: Description("Step-by-step cooking instructions")] string? Instructions,
    [property: Description("Number of servings this recipe yields (1-100)")] int Servings,
    [property: Description("Estimated preparation time in minutes")] int? PrepTimeMinutes,
    [property: Description("List of ingredients (replaces all existing ingredients)")] List<CreateRecipeIngredientRequest> Ingredients
) : IRecipeRequest;

/// <summary>
/// Ingredient details within a recipe response.
/// </summary>
public record RecipeIngredientResponse(
    [property: Description("Ingredient record ID")] Guid Id,
    [property: Description("Referenced product ID")] Guid ProductId,
    [property: Description("Product name")] string ProductName,
    [property: Description("Amount of the product")] decimal Amount,
    [property: Description("Measurement unit")] string Unit
);

/// <summary>
/// Calculated nutrition totals.
/// </summary>
public record NutritionInfo(
    [property: Description("Total calories (kcal)")] decimal Calories,
    [property: Description("Total protein (grams)")] decimal Protein,
    [property: Description("Total carbohydrates (grams)")] decimal Carbs,
    [property: Description("Total fat (grams)")] decimal Fat,
    [property: Description("Total fiber (grams)")] decimal Fiber
);

/// <summary>
/// Recipe details returned from the API, including ingredients and calculated nutrition.
/// </summary>
public record RecipeResponse(
    [property: Description("Unique recipe identifier")] Guid Id,
    [property: Description("Recipe name")] string Name,
    [property: Description("Short description")] string? Description,
    [property: Description("Cooking instructions")] string? Instructions,
    [property: Description("Number of servings")] int Servings,
    [property: Description("Preparation time in minutes")] int? PrepTimeMinutes,
    [property: Description("User ID of the recipe creator")] string CreatedByUserId,
    [property: Description("Creation timestamp (UTC)")] DateTime CreatedAt,
    [property: Description("Last update timestamp (UTC)")] DateTime? UpdatedAt,
    [property: Description("Whether the current user owns this recipe")] bool IsOwner,
    [property: Description("List of ingredients with product details")] List<RecipeIngredientResponse> Ingredients,
    [property: Description("Total nutrition for the entire recipe")] NutritionInfo TotalNutrition,
    [property: Description("Nutrition per single serving")] NutritionInfo NutritionPerServing
)
{
    public static RecipeResponse FromEntity(
        Recipe recipe,
        string currentUserId,
        NutritionInfo totalNutrition,
        NutritionInfo nutritionPerServing)
    {
        return new RecipeResponse(
            recipe.Id,
            recipe.Name,
            recipe.Description,
            recipe.Instructions,
            recipe.Servings,
            recipe.PrepTimeMinutes,
            recipe.CreatedByUserId,
            recipe.CreatedAt,
            recipe.UpdatedAt,
            recipe.CreatedByUserId == currentUserId,
            recipe.Ingredients.Select(i => new RecipeIngredientResponse(
                i.Id,
                i.ProductId,
                i.Product.Name,
                i.Amount,
                i.Unit
            )).ToList(),
            totalNutrition,
            nutritionPerServing
        );
    }
}
