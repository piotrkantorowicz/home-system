namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// Absolute macronutrient totals for a concrete amount of food (a serving, a meal, a day) — as
/// opposed to <see cref="NutritionPer100g"/>, which is a product's density. Calories are kcal, the
/// rest are grams. Values are never <see langword="null"/>; unknown inputs contribute zero.
/// </summary>
/// <param name="Calories">Energy in kcal.</param>
/// <param name="Protein">Protein in grams.</param>
/// <param name="Carbs">Carbohydrates in grams.</param>
/// <param name="Fat">Fat in grams.</param>
/// <param name="Fiber">Fibre in grams.</param>
public sealed record NutritionInfo(
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber)
{
    /// <summary>The additive identity — the starting point for summing servings.</summary>
    public static NutritionInfo Zero => new(0, 0, 0, 0, 0);
}
