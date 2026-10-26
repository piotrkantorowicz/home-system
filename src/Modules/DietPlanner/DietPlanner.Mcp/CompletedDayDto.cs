namespace DietPlanner.Mcp;

/// <summary>
/// The result of <c>complete_day</c>.
/// </summary>
/// <param name="Date">The day completed.</param>
/// <param name="Completed">How many meals changed from Planned to Done.</param>
public sealed record CompletedDayDto(DateOnly Date, int Completed);
