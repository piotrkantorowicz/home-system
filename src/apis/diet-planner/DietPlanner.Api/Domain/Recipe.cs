namespace DietPlanner.Api.Domain;

public class Recipe
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int Servings { get; set; } = 1;
    public int? PrepTimeMinutes { get; set; }
    public required string CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<MealEntry> MealEntries { get; set; } = new List<MealEntry>();

    // Computed property
    public bool IsDeleted => DeletedAt.HasValue;
}
