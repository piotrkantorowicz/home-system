namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// A product's nutrition density per 100 g, as printed on packaging. Every component is optional
/// because label data is often incomplete; calculators treat a missing value as zero.
/// </summary>
/// <param name="Calories">Energy in kcal per 100 g, if known.</param>
/// <param name="Protein">Protein in grams per 100 g, if known.</param>
/// <param name="Carbs">Carbohydrates in grams per 100 g, if known.</param>
/// <param name="Fat">Fat in grams per 100 g, if known.</param>
/// <param name="Fiber">Fibre in grams per 100 g, if known.</param>
public sealed record NutritionPer100g(
    decimal? Calories,
    decimal? Protein,
    decimal? Carbs,
    decimal? Fat,
    decimal? Fiber);
