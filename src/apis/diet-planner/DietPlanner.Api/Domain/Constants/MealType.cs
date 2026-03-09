namespace DietPlanner.Api.Domain.Constants;

public static class MealType
{
    public const string Breakfast = "breakfast";
    public const string Lunch = "lunch";
    public const string Dinner = "dinner";
    public const string Snack = "snack";

    public static readonly string[] All = { Breakfast, Lunch, Dinner, Snack };

    public static bool IsValid(string mealType) =>
        All.Contains(mealType, StringComparer.OrdinalIgnoreCase);
}
