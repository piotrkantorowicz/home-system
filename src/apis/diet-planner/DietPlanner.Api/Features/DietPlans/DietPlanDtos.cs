using System.ComponentModel;
using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Features.DietPlans;

/// <summary>
/// Diet plan summary in list view.
/// </summary>
public class DietPlanSummaryDto
{
    [Description("Unique diet plan identifier")]
    public Guid Id { get; set; }

    [Description("Diet plan name")]
    public required string Name { get; set; }

    [Description("Plan start date (inclusive)")]
    public DateOnly StartDate { get; set; }

    [Description("Plan end date (inclusive)")]
    public DateOnly EndDate { get; set; }

    [Description("Total number of days covered by the plan")]
    public int TotalDays { get; set; }

    [Description("Total number of meal entries in this plan")]
    public int TotalMeals { get; set; }

    [Description("Creation timestamp (UTC)")]
    public DateTime CreatedAt { get; set; }

    public static DietPlanSummaryDto FromEntity(DietPlan plan, int mealCount)
    {
        var totalDays = (plan.EndDate.ToDateTime(TimeOnly.MinValue) -
                        plan.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

        return new DietPlanSummaryDto
        {
            Id = plan.Id,
            Name = plan.Name,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            TotalDays = totalDays,
            TotalMeals = mealCount,
            CreatedAt = plan.CreatedAt
        };
    }
}

/// <summary>
/// Diet plan details.
/// </summary>
public class DietPlanDetailDto
{
    [Description("Unique diet plan identifier")]
    public Guid Id { get; set; }

    [Description("Diet plan name")]
    public required string Name { get; set; }

    [Description("Plan start date (inclusive)")]
    public DateOnly StartDate { get; set; }

    [Description("Plan end date (inclusive)")]
    public DateOnly EndDate { get; set; }

    [Description("Total number of days covered by the plan")]
    public int TotalDays { get; set; }

    [Description("Total number of meal entries in this plan")]
    public int TotalMeals { get; set; }

    [Description("Creation timestamp (UTC)")]
    public DateTime CreatedAt { get; set; }

    public static DietPlanDetailDto FromEntity(DietPlan plan, int mealCount)
    {
        var totalDays = (plan.EndDate.ToDateTime(TimeOnly.MinValue) -
                        plan.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

        return new DietPlanDetailDto
        {
            Id = plan.Id,
            Name = plan.Name,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            TotalDays = totalDays,
            TotalMeals = mealCount,
            CreatedAt = plan.CreatedAt
        };
    }
}

/// <summary>
/// Meal entry with recipe details.
/// </summary>
public class MealEntryDto
{
    [Description("Unique meal entry identifier")]
    public Guid Id { get; set; }

    [Description("Date of the meal (YYYY-MM-DD)")]
    public DateOnly Date { get; set; }

    [Description("Meal type: breakfast, lunch, dinner, snack")]
    public required string MealType { get; set; }

    [Description("Name of the recipe used for this meal")]
    public required string RecipeName { get; set; }

    [Description("Recipe identifier")]
    public Guid RecipeId { get; set; }

    [Description("Number of servings")]
    public decimal Servings { get; set; }

    [Description("Optional notes for this meal")]
    public string? Notes { get; set; }

    [Description("Creation timestamp (UTC)")]
    public DateTime CreatedAt { get; set; }

    public static MealEntryDto FromEntity(MealEntry entry)
    {
        return new MealEntryDto
        {
            Id = entry.Id,
            Date = entry.Date,
            MealType = entry.MealType,
            RecipeName = entry.Recipe?.Name ?? "Unknown",
            RecipeId = entry.RecipeId,
            Servings = entry.Servings,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt
        };
    }
}
