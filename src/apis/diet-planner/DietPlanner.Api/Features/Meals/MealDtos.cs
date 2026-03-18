using System.ComponentModel;
using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Features.Meals;

/// <summary>
/// Request to create a new meal entry.
/// </summary>
public record CreateMealEntryRequest(
    [property: Description("Date of the meal (YYYY-MM-DD)")]
    DateOnly Date,
    [property: Description("Meal type: breakfast, lunch, dinner, snack")]
    string MealType,
    [property: Description("Recipe identifier")]
    Guid RecipeId,
    [property: Description("Number of servings")]
    decimal Servings = 1m,
    [property: Description("Optional notes")]
    string? Notes = null,
    [property: Description("Optional time of the meal")]
    TimeOnly? MealTime = null,
    [property: Description("Display order within the meal type slot")]
    int? SequenceOrder = null
) : IMealEntryRequest;

/// <summary>
/// Request to update an existing meal entry.
/// </summary>
public record UpdateMealEntryRequest(
    [property: Description("Date of the meal (YYYY-MM-DD)")]
    DateOnly Date,
    [property: Description("Meal type: breakfast, lunch, dinner, snack")]
    string MealType,
    [property: Description("Recipe identifier")]
    Guid RecipeId,
    [property: Description("Number of servings")]
    decimal Servings = 1m,
    [property: Description("Optional notes")]
    string? Notes = null,
    [property: Description("Optional time of the meal")]
    TimeOnly? MealTime = null,
    [property: Description("Display order within the meal type slot")]
    int? SequenceOrder = null
) : IMealEntryRequest;

public interface IMealEntryRequest
{
    DateOnly Date { get; }
    string MealType { get; }
    Guid RecipeId { get; }
    decimal Servings { get; }
    string? Notes { get; }
    TimeOnly? MealTime { get; }
    int? SequenceOrder { get; }
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

    [Description("Optional time of the meal")]
    public TimeOnly? MealTime { get; set; }

    [Description("Display order within the meal type slot")]
    public int? SequenceOrder { get; set; }

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
            MealTime = entry.MealTime,
            SequenceOrder = entry.SequenceOrder,
            CreatedAt = entry.CreatedAt
        };
    }
}
