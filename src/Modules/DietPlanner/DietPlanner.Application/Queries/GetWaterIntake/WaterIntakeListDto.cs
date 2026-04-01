namespace DietPlanner.Application.Queries.GetWaterIntake;

public sealed record WaterIntakeListDto(DateOnly Date, int TotalMl, IReadOnlyList<WaterIntakeEntryDto> Entries);
