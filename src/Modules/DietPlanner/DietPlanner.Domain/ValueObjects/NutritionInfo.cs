namespace DietPlanner.Domain.ValueObjects;

public sealed record NutritionInfo(
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal Fiber)
{
    public static NutritionInfo Zero => new(0, 0, 0, 0, 0);
}
