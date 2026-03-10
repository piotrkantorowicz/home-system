namespace DietPlanner.Api.Domain;

public class UserGoal
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public int? DailyCalorieTarget { get; set; }
    public decimal? ProteinGrams { get; set; }
    public decimal? CarbsGrams { get; set; }
    public decimal? FatGrams { get; set; }
    public decimal? FiberGrams { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
