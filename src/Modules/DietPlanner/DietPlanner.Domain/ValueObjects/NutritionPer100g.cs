namespace DietPlanner.Domain.ValueObjects;

public sealed record NutritionPer100g(
    decimal? Calories,
    decimal? Protein,
    decimal? Carbs,
    decimal? Fat,
    decimal? Fiber);
