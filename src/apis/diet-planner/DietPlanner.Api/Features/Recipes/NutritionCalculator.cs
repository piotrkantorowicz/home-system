using DietPlanner.Api.Common.Utils;
using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Features.Recipes;

public class NutritionCalculator : INutritionCalculator
{
    public NutritionInfo CalculateTotalNutrition(Recipe recipe)
    {
        if (recipe.Ingredients == null || !recipe.Ingredients.Any())
        {
            return new NutritionInfo(0, 0, 0, 0);
        }

        decimal totalCalories = 0;
        decimal totalProtein = 0;
        decimal totalCarbs = 0;
        decimal totalFat = 0;

        foreach (var ingredient in recipe.Ingredients)
        {
            var product = ingredient.Product;
            if (product == null) continue;

            var grams = UnitConverter.ConvertToGrams(ingredient.Amount, ingredient.Unit, product);
            var factor = grams / 100m;

            totalCalories += (product.CaloriesPer100g ?? 0) * factor;
            totalProtein += (product.ProteinPer100g ?? 0) * factor;
            totalCarbs += (product.CarbsPer100g ?? 0) * factor;
            totalFat += (product.FatPer100g ?? 0) * factor;
        }

        return new NutritionInfo(
            Math.Round(totalCalories, 1),
            Math.Round(totalProtein, 1),
            Math.Round(totalCarbs, 1),
            Math.Round(totalFat, 1)
        );
    }

    public NutritionInfo CalculateNutritionPerServing(Recipe recipe)
    {
        var total = CalculateTotalNutrition(recipe);
        var servings = Math.Max(recipe.Servings, 1);

        return new NutritionInfo(
            Math.Round(total.Calories / servings, 1),
            Math.Round(total.Protein / servings, 1),
            Math.Round(total.Carbs / servings, 1),
            Math.Round(total.Fat / servings, 1)
        );
    }

    public NutritionInfo CalculateNutritionForServings(Recipe recipe, decimal servings)
    {
        var perServing = CalculateNutritionPerServing(recipe);

        return new NutritionInfo(
            Math.Round(perServing.Calories * servings, 1),
            Math.Round(perServing.Protein * servings, 1),
            Math.Round(perServing.Carbs * servings, 1),
            Math.Round(perServing.Fat * servings, 1)
        );
    }
}
