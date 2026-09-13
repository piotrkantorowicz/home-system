namespace DietPlanner.Domain.Services;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Derives a recipe's macros from its ingredients. Products are supplied through a lookup so the
/// caller controls loading (batch-fetch, then resolve in memory); an ingredient whose product is
/// missing contributes nothing.
/// </summary>
public interface INutritionCalculator
{
    /// <summary>Macros for the whole recipe, i.e. all of its servings.</summary>
    /// <param name="recipe">The recipe and its ingredient lines.</param>
    /// <param name="productLookup">Resolves an ingredient's product, or <see langword="null"/> if unavailable.</param>
    NutritionInfo CalculateTotalNutrition(Recipe recipe, Func<ProductId, Product?> productLookup);
    /// <summary>Macros for one serving: the total divided by <c>Recipe.Servings</c>.</summary>
    /// <param name="recipe">The recipe and its ingredient lines.</param>
    /// <param name="productLookup">Resolves an ingredient's product, or <see langword="null"/> if unavailable.</param>
    NutritionInfo CalculateNutritionPerServing(Recipe recipe, Func<ProductId, Product?> productLookup);
    /// <summary>Macros for an arbitrary number of servings, e.g. a meal entry's portion.</summary>
    /// <param name="recipe">The recipe and its ingredient lines.</param>
    /// <param name="servings">How many servings were (or will be) eaten.</param>
    /// <param name="productLookup">Resolves an ingredient's product, or <see langword="null"/> if unavailable.</param>
    NutritionInfo CalculateNutritionForServings(Recipe recipe, decimal servings, Func<ProductId, Product?> productLookup);
}
