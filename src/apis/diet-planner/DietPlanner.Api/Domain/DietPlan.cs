namespace DietPlanner.Api.Domain;

public class DietPlan
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<MealEntry> MealEntries { get; set; } = new List<MealEntry>();

    // Computed property
    public bool IsDeleted => DeletedAt.HasValue;
}
