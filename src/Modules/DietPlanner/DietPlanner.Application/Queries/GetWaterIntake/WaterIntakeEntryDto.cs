namespace DietPlanner.Application.Queries.GetWaterIntake;

/// <summary>
/// One logged drink.
/// </summary>
/// <param name="Id">Identifier of the entry.</param>
/// <param name="AmountMl">Volume in millilitres.</param>
/// <param name="Timestamp">When it was logged, UTC.</param>
/// <param name="Note">Optional free-text note.</param>
public sealed record WaterIntakeEntryDto(Guid Id, int AmountMl, DateTime Timestamp, string? Note);
