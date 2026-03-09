using System.ComponentModel;
using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Features.Goals;

/// <summary>
/// Request to create or update user's daily nutrition goals.
/// </summary>
public record UpsertGoalRequest(
    [property: Description("Daily calorie intake target (kcal)")]
    int? DailyCalorieTarget,
    [property: Description("Daily protein target (grams)")]
    decimal? ProteinGrams,
    [property: Description("Daily carbohydrate target (grams)")]
    decimal? CarbsGrams,
    [property: Description("Daily fat target (grams)")]
    decimal? FatGrams,
    [property: Description("Daily fiber target (grams)")]
    decimal? FiberGrams
);

/// <summary>
/// User's daily nutrition goals.
/// </summary>
public class GoalResponse
{
    [Description("Unique goal record identifier")]
    public Guid Id { get; set; }

    [Description("Daily calorie intake target (kcal)")]
    public int? DailyCalorieTarget { get; set; }

    [Description("Daily protein target (grams)")]
    public decimal? ProteinGrams { get; set; }

    [Description("Daily carbohydrate target (grams)")]
    public decimal? CarbsGrams { get; set; }

    [Description("Daily fat target (grams)")]
    public decimal? FatGrams { get; set; }

    [Description("Daily fiber target (grams)")]
    public decimal? FiberGrams { get; set; }

    [Description("Creation timestamp (UTC)")]
    public DateTime CreatedAt { get; set; }

    [Description("Last update timestamp (UTC)")]
    public DateTime? UpdatedAt { get; set; }

    public static GoalResponse FromEntity(UserGoal goal)
    {
        return new GoalResponse
        {
            Id = goal.Id,
            DailyCalorieTarget = goal.DailyCalorieTarget,
            ProteinGrams = goal.ProteinGrams,
            CarbsGrams = goal.CarbsGrams,
            FatGrams = goal.FatGrams,
            FiberGrams = goal.FiberGrams,
            CreatedAt = goal.CreatedAt,
            UpdatedAt = goal.UpdatedAt
        };
    }

    public static GoalResponse Empty()
    {
        return new GoalResponse
        {
            Id = Guid.Empty,
            DailyCalorieTarget = null,
            ProteinGrams = null,
            CarbsGrams = null,
            FatGrams = null,
            FiberGrams = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }
}
