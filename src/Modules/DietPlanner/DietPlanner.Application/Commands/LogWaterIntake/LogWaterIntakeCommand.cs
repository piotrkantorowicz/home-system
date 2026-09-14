namespace DietPlanner.Application.Commands.LogWaterIntake;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Logs a drink for the caller and returns the new entry's id.
/// </summary>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
/// <param name="Date">The calendar day the drink counts towards.</param>
/// <param name="AmountMl">Volume in millilitres; positive.</param>
/// <param name="Note">Optional free-text note.</param>
public sealed record LogWaterIntakeCommand(
    string UserId,
    DateOnly Date,
    int AmountMl,
    string? Note) : ICommand<Guid>;
