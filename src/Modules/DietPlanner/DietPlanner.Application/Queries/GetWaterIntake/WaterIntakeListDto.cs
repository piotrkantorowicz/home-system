namespace DietPlanner.Application.Queries.GetWaterIntake;

/// <summary>
/// A day's drinks, newest first, with the total.
/// </summary>
/// <param name="Date">The calendar day.</param>
/// <param name="TotalMl">Sum of all entries in millilitres.</param>
/// <param name="Entries">The drinks logged that day.</param>
public sealed record WaterIntakeListDto(DateOnly Date, int TotalMl, IReadOnlyList<WaterIntakeEntryDto> Entries);
