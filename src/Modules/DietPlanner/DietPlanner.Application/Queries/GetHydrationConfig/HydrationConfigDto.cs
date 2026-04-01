namespace DietPlanner.Application.Queries.GetHydrationConfig;

public sealed record HydrationConfigDto(
    Guid Id,
    string UserId,
    int DailyWaterTargetMl,
    int GlassSizeMl,
    bool TrackWaterIntake,
    DateTime CreatedAt,
    DateTime UpdatedAt);
