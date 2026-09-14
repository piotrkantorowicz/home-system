namespace DietPlanner.Application.Queries.GetNutritionSummary;

/// <summary>
/// Macro totals of everything planned or eaten on one day.
/// </summary>
/// <param name="Date">The calendar day.</param>
/// <param name="Calories">Energy in kcal.</param>
/// <param name="Protein">Protein in grams.</param>
/// <param name="Carbs">Carbohydrates in grams.</param>
/// <param name="Fat">Fat in grams.</param>
/// <param name="Fiber">Fibre in grams.</param>
public sealed record DailyNutritionDto(
    DateOnly Date,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber);
