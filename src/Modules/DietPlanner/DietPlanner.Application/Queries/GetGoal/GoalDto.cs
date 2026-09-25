namespace DietPlanner.Application.Queries.GetGoal;

/// <summary>
/// A user's daily nutrition targets.
/// </summary>
/// <param name="Id">Identifier of the goal.</param>
/// <param name="PersonId">Person identifier of the owner.</param>
/// <param name="DailyCalorieTarget">Daily energy target in kcal, if set.</param>
/// <param name="ProteinGrams">Daily protein target in grams, if set.</param>
/// <param name="CarbsGrams">Daily carbohydrate target in grams, if set.</param>
/// <param name="FatGrams">Daily fat target in grams, if set.</param>
/// <param name="FiberGrams">Daily fibre target in grams, if set.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC; <see langword="null"/> if never changed.</param>
public sealed record GoalDto(
    Guid Id,
    Guid PersonId,
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
