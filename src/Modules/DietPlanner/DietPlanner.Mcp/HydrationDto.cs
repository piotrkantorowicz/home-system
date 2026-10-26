namespace DietPlanner.Mcp;

using DietPlanner.Application.Queries.GetHydrationConfig;
using DietPlanner.Application.Queries.GetWaterIntake;

/// <summary>
/// The caller's hydration settings and one day's logged drinks; settings and drinks are stored separately.
/// </summary>
/// <param name="Config">Daily target, glass size and whether tracking is on; <see langword="null"/> until set with update_hydration_config.</param>
/// <param name="Intake">Drinks logged on the day, with their total in millilitres.</param>
public sealed record HydrationDto(HydrationConfigDto? Config, WaterIntakeListDto Intake);
