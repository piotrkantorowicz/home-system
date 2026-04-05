namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

public sealed class HydrationConfig : AggregateRoot<HydrationConfigId>
{
    private HydrationConfig() { }

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

    public string UserId { get; private set; } = default!;
    public int DailyWaterTargetMl { get; private set; }
    public int GlassSizeMl { get; private set; }
    public bool TrackWaterIntake { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Update(int dailyWaterTargetMl, int glassSizeMl, bool trackWaterIntake)
    {
        DailyWaterTargetMl = dailyWaterTargetMl;
        GlassSizeMl = glassSizeMl;
        TrackWaterIntake = trackWaterIntake;
        UpdatedAt = DateTime.UtcNow;
    }
}
