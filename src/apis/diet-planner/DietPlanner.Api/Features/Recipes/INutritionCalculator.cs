using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Features.Recipes;

public interface INutritionCalculator
{
    NutritionInfo CalculateTotalNutrition(Recipe recipe);
    NutritionInfo CalculateNutritionPerServing(Recipe recipe);
    NutritionInfo CalculateNutritionForServings(Recipe recipe, decimal servings);
}
