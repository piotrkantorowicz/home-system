namespace DietPlanner.Application.Commands.UpdateHydrationConfig;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Replaces the caller's hydration preferences, creating them if they do not exist yet.
/// </summary>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="DailyWaterTargetMl">Daily target in millilitres; positive.</param>
/// <param name="GlassSizeMl">Volume one "glass" tap logs, in millilitres; positive.</param>
/// <param name="TrackWaterIntake">Whether water tracking and its reminders are enabled.</param>
public sealed record UpdateHydrationConfigCommand(
    Guid PersonId,
    int DailyWaterTargetMl,
    int GlassSizeMl,
    bool TrackWaterIntake) : ICommand;
