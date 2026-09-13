namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

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
        decimal? fiberGrams,
        decimal? targetWeightKg = null)
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
            TargetWeightKg = targetWeightKg,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public string UserId { get; private set; } = default!;
    public int? DailyCalorieTarget { get; private set; }
    public decimal? ProteinGrams { get; private set; }
    public decimal? CarbsGrams { get; private set; }
    public decimal? FatGrams { get; private set; }
    public decimal? FiberGrams { get; private set; }
    public decimal? TargetWeightKg { get; private set; }
    public DateTime? MilestoneAchievedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void Update(
        int? dailyCalorieTarget,
        decimal? proteinGrams,
        decimal? carbsGrams,
        decimal? fatGrams,
        decimal? fiberGrams,
        decimal? targetWeightKg = null)
    {
        DailyCalorieTarget = dailyCalorieTarget;
        ProteinGrams = proteinGrams;
        CarbsGrams = carbsGrams;
        FatGrams = fatGrams;
        FiberGrams = fiberGrams;
        TargetWeightKg = targetWeightKg;
        UpdatedAt = DateTime.UtcNow;
    }

    // True iff a weight milestone is freshly reached on this evaluation:
    // a target is set, it has not been previously achieved, and the current
    // weight has reached or crossed below the target.
    public bool ShouldEmitWeightMilestone(decimal currentWeightKg)
        => TargetWeightKg is not null
           && MilestoneAchievedAt is null
           && currentWeightKg <= TargetWeightKg.Value;

    // Idempotent — no-op if already achieved.
    public void MarkMilestoneAchieved(DateTime utcNow)
    {
        if (MilestoneAchievedAt is not null) return;
        MilestoneAchievedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
