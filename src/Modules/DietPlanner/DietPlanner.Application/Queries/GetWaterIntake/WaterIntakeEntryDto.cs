namespace DietPlanner.Application.Queries.GetWaterIntake;

public sealed record WaterIntakeEntryDto(Guid Id, int AmountMl, DateTime Timestamp, string? Note);
