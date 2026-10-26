namespace DietPlanner.Application.Commands.LogWaterIntake;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Logs a drink for the caller and returns the new entry's id.
/// </summary>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="Date">The calendar day the drink counts towards.</param>
/// <param name="AmountMl">Volume in millilitres; positive.</param>
/// <param name="Note">Optional free-text note.</param>
/// <param name="OperationKey">Optional idempotency key: a retry with the same key and fields returns the original id instead of logging again; the same key with different fields is a conflict.</param>
public sealed record LogWaterIntakeCommand(
    Guid PersonId,
    DateOnly Date,
    int AmountMl,
    string? Note,
    string? OperationKey = null) : ICommand<Guid>;
