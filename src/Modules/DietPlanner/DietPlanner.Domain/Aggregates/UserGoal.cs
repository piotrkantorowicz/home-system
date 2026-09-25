namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A user's daily nutrition targets plus an optional target weight. One goal per user. The weight
/// milestone is emitted once: <see cref="ShouldEmitWeightMilestone"/> is true only until
/// <see cref="MarkMilestoneAchieved"/> has recorded it.
/// </summary>
public sealed class UserGoal : AggregateRoot<UserGoalId>
{
    private UserGoal() { }

    /// <summary>Creates a goal; any target may be left unset.</summary>
    /// <param name="id">Identifier for the new goal.</param>
    /// <param name="personId">Person identifier of the owner; required.</param>
    /// <param name="dailyCalorieTarget">Daily energy target in kcal.</param>
    /// <param name="proteinGrams">Daily protein target in grams.</param>
    /// <param name="carbsGrams">Daily carbohydrate target in grams.</param>
    /// <param name="fatGrams">Daily fat target in grams.</param>
    /// <param name="fiberGrams">Daily fibre target in grams.</param>
    /// <param name="targetWeightKg">Weight to reach, in kilograms; enables the milestone notification.</param>
    /// <exception cref="ArgumentException"><paramref name="personId"/> is empty.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static UserGoal Create(
        UserGoalId id,
        Guid personId,
        DateTime now,
        int? dailyCalorieTarget,
        decimal? proteinGrams,
        decimal? carbsGrams,
        decimal? fatGrams,
        decimal? fiberGrams,
        decimal? targetWeightKg = null)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        return new UserGoal
        {
            Id = id,
            PersonId = personId,
            DailyCalorieTarget = dailyCalorieTarget,
            ProteinGrams = proteinGrams,
            CarbsGrams = carbsGrams,
            FatGrams = fatGrams,
            FiberGrams = fiberGrams,
            TargetWeightKg = targetWeightKg,
            CreatedAt = now,
        };
    }

    /// <summary>Person identifier of the owner.</summary>
    public Guid PersonId { get; private set; }
    /// <summary>Daily energy target in kcal, if set.</summary>
    public int? DailyCalorieTarget { get; private set; }
    /// <summary>Daily protein target in grams, if set.</summary>
    public decimal? ProteinGrams { get; private set; }
    /// <summary>Daily carbohydrate target in grams, if set.</summary>
    public decimal? CarbsGrams { get; private set; }
    /// <summary>Daily fat target in grams, if set.</summary>
    public decimal? FatGrams { get; private set; }
    /// <summary>Daily fibre target in grams, if set.</summary>
    public decimal? FiberGrams { get; private set; }
    /// <summary>Weight to reach in kilograms, if set.</summary>
    public decimal? TargetWeightKg { get; private set; }
    /// <summary>When the target weight was first reached, UTC; <see langword="null"/> until then.</summary>
    public DateTime? MilestoneAchievedAt { get; private set; }
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last change, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Replaces every target at once; pass <see langword="null"/> to clear one. Does not reset the milestone.</summary>
    /// <param name="dailyCalorieTarget">New daily energy target in kcal.</param>
    /// <param name="proteinGrams">New daily protein target in grams.</param>
    /// <param name="carbsGrams">New daily carbohydrate target in grams.</param>
    /// <param name="fatGrams">New daily fat target in grams.</param>
    /// <param name="fiberGrams">New daily fibre target in grams.</param>
    /// <param name="targetWeightKg">New target weight in kilograms.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public void Update(
        int? dailyCalorieTarget,
        decimal? proteinGrams,
        decimal? carbsGrams,
        decimal? fatGrams,
        decimal? fiberGrams,
        DateTime now,
        decimal? targetWeightKg = null)
    {
        DailyCalorieTarget = dailyCalorieTarget;
        ProteinGrams = proteinGrams;
        CarbsGrams = carbsGrams;
        FatGrams = fatGrams;
        FiberGrams = fiberGrams;
        TargetWeightKg = targetWeightKg;
        UpdatedAt = now;
    }

    /// <summary>
    /// True iff a weight milestone is freshly reached on this evaluation: a target is set, it has
    /// not been previously achieved, and the current weight has reached or crossed below the target.
    /// </summary>
    /// <param name="currentWeightKg">The user's latest weight in kilograms.</param>
    public bool ShouldEmitWeightMilestone(decimal currentWeightKg)
        => TargetWeightKg is not null
           && MilestoneAchievedAt is null
           && currentWeightKg <= TargetWeightKg.Value;

    /// <summary>Records that the target weight was reached. Idempotent — a no-op if already achieved.</summary>
    /// <param name="utcNow">The evaluation time, UTC.</param>
    public void MarkMilestoneAchieved(DateTime utcNow)
    {
        if (MilestoneAchievedAt is not null) return;
        MilestoneAchievedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
