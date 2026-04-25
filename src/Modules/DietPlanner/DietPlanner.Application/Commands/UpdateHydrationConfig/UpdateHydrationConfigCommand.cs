namespace DietPlanner.Application.Commands.UpdateHydrationConfig;

using Shared.Abstractions.Cqrs;

public sealed record UpdateHydrationConfigCommand(
    string UserId,
    int DailyWaterTargetMl,
    int GlassSizeMl,
    bool TrackWaterIntake) : ICommand;
