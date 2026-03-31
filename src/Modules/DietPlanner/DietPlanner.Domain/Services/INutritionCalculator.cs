namespace DietPlanner.Domain.Services;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public interface INutritionCalculator
{
    NutritionInfo CalculateTotalNutrition(Recipe recipe, Func<ProductId, Product?> productLookup);
    NutritionInfo CalculateNutritionPerServing(Recipe recipe, Func<ProductId, Product?> productLookup);
    NutritionInfo CalculateNutritionForServings(Recipe recipe, decimal servings, Func<ProductId, Product?> productLookup);
}
