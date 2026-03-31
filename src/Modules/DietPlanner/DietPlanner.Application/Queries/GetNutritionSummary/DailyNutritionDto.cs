namespace DietPlanner.Application.Queries.GetNutritionSummary;

public sealed record DailyNutritionDto(
    DateOnly Date,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber);
