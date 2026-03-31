namespace DietPlanner.Domain.Aggregates;

using Shared.Abstractions.Domain;
using DietPlanner.Domain.ValueObjects;

public sealed class UserGoal : AggregateRoot<UserGoalId>
{
    private UserGoal() { }

    public static UserGoal Create(
        UserGoalId id,
        string userId,
        int? dailyCalorieTarget,
        decimal? proteinGrams,
        decimal? carbsGrams,
        decimal? fatGrams,
        decimal? fiberGrams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new UserGoal
        {
            Id = id,
            UserId = userId,
            DailyCalorieTarget = dailyCalorieTarget,
            ProteinGrams = proteinGrams,
            CarbsGrams = carbsGrams,
            FatGrams = fatGrams,
            FiberGrams = fiberGrams,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string UserId { get; private set; } = default!;
    public int? DailyCalorieTarget { get; private set; }
    public decimal? ProteinGrams { get; private set; }
    public decimal? CarbsGrams { get; private set; }
    public decimal? FatGrams { get; private set; }
    public decimal? FiberGrams { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void Update(
        int? dailyCalorieTarget,
        decimal? proteinGrams,
        decimal? carbsGrams,
        decimal? fatGrams,
        decimal? fiberGrams)
    {
        DailyCalorieTarget = dailyCalorieTarget;
        ProteinGrams = proteinGrams;
        CarbsGrams = carbsGrams;
        FatGrams = fatGrams;
        FiberGrams = fiberGrams;
        UpdatedAt = DateTime.UtcNow;
    }
}
