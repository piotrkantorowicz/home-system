namespace DietPlanner.Domain.Constants;

/// <summary>
/// The fixed vocabulary of meal types a recipe or entry can be tagged with. Stored as lower-case
/// strings; <see cref="IsValid"/> accepts any casing.
/// </summary>
public static class MealType
{
    /// <summary>The first meal of the day.</summary>
    public const string Breakfast = "breakfast";

    /// <summary>The midday meal.</summary>
    public const string Lunch = "lunch";

    /// <summary>The evening meal.</summary>
    public const string Dinner = "dinner";

    /// <summary>Anything eaten between main meals.</summary>
    public const string Snack = "snack";

    /// <summary>All accepted values, in display order.</summary>
    public static readonly string[] All = [Breakfast, Lunch, Dinner, Snack];

    /// <summary>Whether the value is one of <see cref="All"/>, ignoring case.</summary>
    /// <param name="mealType">The value to check.</param>
    public static bool IsValid(string mealType) => All.Contains(mealType, StringComparer.OrdinalIgnoreCase);
}
