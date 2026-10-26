namespace DietPlanner.Mcp;

/// <summary>
/// The drink a <c>log_water_intake</c> call logged, or logged on an earlier call with the same operation key.
/// </summary>
/// <param name="IntakeId">Id of the drink entry, for delete_water_intake.</param>
/// <param name="Date">Day it counts towards.</param>
/// <param name="AmountMl">Volume in millilitres.</param>
public sealed record LoggedWaterIntakeDto(Guid IntakeId, DateOnly Date, int AmountMl);
