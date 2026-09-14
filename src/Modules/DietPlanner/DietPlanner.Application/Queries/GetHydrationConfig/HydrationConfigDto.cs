namespace DietPlanner.Application.Queries.GetHydrationConfig;

/// <summary>
/// A user's hydration preferences.
/// </summary>
/// <param name="Id">Identifier of the config.</param>
/// <param name="UserId">Auth subject of the owner.</param>
/// <param name="DailyWaterTargetMl">Daily target in millilitres.</param>
/// <param name="GlassSizeMl">Volume one "glass" tap logs, in millilitres.</param>
/// <param name="TrackWaterIntake">Whether water tracking and its reminders are enabled.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC.</param>
public sealed record HydrationConfigDto(
    Guid Id,
    string UserId,
    int DailyWaterTargetMl,
    int GlassSizeMl,
    bool TrackWaterIntake,
    DateTime CreatedAt,
    DateTime UpdatedAt);
