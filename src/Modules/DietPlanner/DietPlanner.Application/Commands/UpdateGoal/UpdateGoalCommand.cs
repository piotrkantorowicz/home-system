namespace DietPlanner.Application.Commands.UpdateGoal;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Replaces the caller's nutrition targets; the goal must exist.
/// </summary>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
/// <param name="DailyCalorieTarget">Daily energy target in kcal, or <see langword="null"/> to leave it unset.</param>
/// <param name="ProteinGrams">Daily protein target in grams, or <see langword="null"/>.</param>
/// <param name="CarbsGrams">Daily carbohydrate target in grams, or <see langword="null"/>.</param>
/// <param name="FatGrams">Daily fat target in grams, or <see langword="null"/>.</param>
/// <param name="FiberGrams">Daily fibre target in grams, or <see langword="null"/>.</param>
public sealed record UpdateGoalCommand(
    string UserId,
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams) : ICommand;
