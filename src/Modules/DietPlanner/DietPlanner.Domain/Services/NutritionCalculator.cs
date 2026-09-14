namespace DietPlanner.Domain.Services;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Default <see cref="INutritionCalculator"/>: converts each ingredient to grams with
/// <see cref="UnitConverter"/>, scales the product's per-100 g values and sums them. Unknown
/// nutrition components count as zero.
/// </summary>
public sealed class NutritionCalculator : INutritionCalculator
{
    /// <inheritdoc />
    public NutritionInfo CalculateTotalNutrition(Recipe recipe, Func<ProductId, Product?> productLookup)
    {
        decimal totalCalories = 0, totalProtein = 0, totalCarbs = 0, totalFat = 0, totalFiber = 0;

        foreach (var ingredient in recipe.Ingredients)
        {
            var product = productLookup(ingredient.ProductId);
            if (product is null) continue;

            var grams = UnitConverter.ConvertToGrams(
                ingredient.Amount, ingredient.Unit, product.DensityGramsPerMl, product.GramPerPiece);

            var factor = grams / 100m;
            totalCalories += (product.Nutrition.Calories ?? 0) * factor;
            totalProtein += (product.Nutrition.Protein ?? 0) * factor;
            totalCarbs += (product.Nutrition.Carbs ?? 0) * factor;
            totalFat += (product.Nutrition.Fat ?? 0) * factor;
            totalFiber += (product.Nutrition.Fiber ?? 0) * factor;
        }

        return new NutritionInfo(
            Math.Round(totalCalories, 1),
            Math.Round(totalProtein, 1),
            Math.Round(totalCarbs, 1),
            Math.Round(totalFat, 1),
            Math.Round(totalFiber, 1));
    }

    /// <inheritdoc />
    public NutritionInfo CalculateNutritionPerServing(Recipe recipe, Func<ProductId, Product?> productLookup)
    {
        var total = CalculateTotalNutrition(recipe, productLookup);
        var servings = Math.Max(recipe.Servings, 1);

        return new NutritionInfo(
            Math.Round(total.Calories / servings, 1),
            Math.Round(total.Protein / servings, 1),
            Math.Round(total.Carbs / servings, 1),
            Math.Round(total.Fat / servings, 1),
            Math.Round(total.Fiber / servings, 1));
    }

    /// <inheritdoc />
    public NutritionInfo CalculateNutritionForServings(Recipe recipe, decimal servings, Func<ProductId, Product?> productLookup)
    {
        var perServing = CalculateNutritionPerServing(recipe, productLookup);

        return new NutritionInfo(
            Math.Round(perServing.Calories * servings, 1),
            Math.Round(perServing.Protein * servings, 1),
            Math.Round(perServing.Carbs * servings, 1),
            Math.Round(perServing.Fat * servings, 1),
            Math.Round(perServing.Fiber * servings, 1));
    }
}
