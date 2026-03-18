namespace DietPlanner.Api.Domain;

public class MealEntry
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public DateOnly Date { get; set; }
    public required string MealType { get; set; }
    public Guid RecipeId { get; set; }
    public decimal Servings { get; set; } = 1m;
    public string? Notes { get; set; }
    public TimeOnly? MealTime { get; set; }
    public int? SequenceOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Recipe Recipe { get; set; } = null!;
}
