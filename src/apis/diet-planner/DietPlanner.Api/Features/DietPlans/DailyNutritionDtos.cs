using System.ComponentModel;
using DietPlanner.Api.Features.Goals;

namespace DietPlanner.Api.Features.DietPlans;

/// <summary>
/// Nutrition totals for a single day.
/// </summary>
public class DailyNutritionDto
{
    [Description("Date (YYYY-MM-DD)")]
    public DateOnly Date { get; set; }

    [Description("Total calories for the day (kcal)")]
    public decimal TotalCalories { get; set; }

    [Description("Total protein for the day (grams)")]
    public decimal TotalProtein { get; set; }

    [Description("Total carbohydrates for the day (grams)")]
    public decimal TotalCarbs { get; set; }

    [Description("Total fat for the day (grams)")]
    public decimal TotalFat { get; set; }

    [Description("Total fiber for the day (grams)")]
    public decimal TotalFiber { get; set; }

    [Description("Number of meals planned for this day")]
    public int MealCount { get; set; }

    [Description("Calorie goal status: met, partial, missed, exceeded, no_goal")]
    public string CaloriesStatus { get; set; } = "no_goal";

    [Description("Protein goal status: met, partial, missed, exceeded, no_goal")]
    public string ProteinStatus { get; set; } = "no_goal";

    [Description("Carbs goal status: met, partial, missed, exceeded, no_goal")]
    public string CarbsStatus { get; set; } = "no_goal";

    [Description("Fat goal status: met, partial, missed, exceeded, no_goal")]
    public string FatStatus { get; set; } = "no_goal";

    [Description("Fiber goal status: met, partial, missed, exceeded, no_goal")]
    public string FiberStatus { get; set; } = "no_goal";
}

/// <summary>
/// Daily nutrition totals with goal comparison for a date range.
/// </summary>
public class DailyNutritionResponse
{
    [Description("User's nutrition goals (null if not set)")]
    public GoalResponse? Goals { get; set; }

    [Description("Daily nutrition breakdown with goal status")]
    public List<DailyNutritionDto> Days { get; set; } = [];
}
