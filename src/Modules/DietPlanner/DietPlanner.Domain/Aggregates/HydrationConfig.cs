namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A user's hydration preferences: daily water target, the glass size used for one-tap logging,
/// and whether water tracking (and its reminders) is on at all. One config per user, created with
/// sensible defaults (2500 ml / 250 ml / on) on first access.
/// </summary>
public sealed class HydrationConfig : AggregateRoot<HydrationConfigId>
{
    private HydrationConfig() { }

    /// <summary>Creates the config, defaulting to 2500 ml a day in 250 ml glasses with tracking on.</summary>
    /// <param name="id">Identifier for the new config.</param>
    /// <param name="userId">Auth subject of the owner; required.</param>
    /// <param name="dailyWaterTargetMl">Daily target in millilitres.</param>
    /// <param name="glassSizeMl">Volume one "glass" tap logs, in millilitres.</param>
    /// <param name="trackWaterIntake">Whether water tracking and reminders are enabled.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    public static HydrationConfig Create(
        HydrationConfigId id,
        string userId,
        int dailyWaterTargetMl = 2500,
        int glassSizeMl = 250,
        bool trackWaterIntake = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new HydrationConfig
        {
            Id = id,
            UserId = userId,
            DailyWaterTargetMl = dailyWaterTargetMl,
            GlassSizeMl = glassSizeMl,
            TrackWaterIntake = trackWaterIntake,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Auth subject of the owner.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>Daily target in millilitres.</summary>
    public int DailyWaterTargetMl { get; private set; }
    /// <summary>Volume one "glass" tap logs, in millilitres.</summary>
    public int GlassSizeMl { get; private set; }
    /// <summary>Whether water tracking and its reminders are enabled.</summary>
    public bool TrackWaterIntake { get; private set; }
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last change, UTC; equals <see cref="CreatedAt"/> until the first <see cref="Update"/>.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Replaces all three preferences at once.</summary>
    /// <param name="dailyWaterTargetMl">New daily target in millilitres.</param>
    /// <param name="glassSizeMl">New glass volume in millilitres.</param>
    /// <param name="trackWaterIntake">Whether tracking stays enabled.</param>
    public void Update(int dailyWaterTargetMl, int glassSizeMl, bool trackWaterIntake)
    {
        DailyWaterTargetMl = dailyWaterTargetMl;
        GlassSizeMl = glassSizeMl;
        TrackWaterIntake = trackWaterIntake;
        UpdatedAt = DateTime.UtcNow;
    }
}
