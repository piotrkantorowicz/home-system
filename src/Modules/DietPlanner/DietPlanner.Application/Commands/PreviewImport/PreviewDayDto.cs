namespace DietPlanner.Application.Commands.PreviewImport;

/// <summary>
/// The meals an import plans on one day.
/// </summary>
/// <param name="Date">Calendar day.</param>
/// <param name="Meals">Meals in slot order.</param>
public sealed record PreviewDayDto(DateOnly Date, IReadOnlyList<PreviewMealDto> Meals);
