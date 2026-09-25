namespace DietPlanner.Application.Commands.LogWeightEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Records the caller's weight for a day. If the day already has an entry its weight is corrected instead of adding a second one; either way the profile's current weight is refreshed.
/// </summary>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="Date">The day of the weigh-in; today or earlier.</param>
/// <param name="WeightKg">Weight in kilograms, 0.1–999.</param>
public sealed record LogWeightEntryCommand(
    Guid PersonId,
    DateOnly Date,
    decimal WeightKg) : ICommand<LogWeightEntryResult>;

/// <summary>
/// Outcome of logging a weight.
/// </summary>
/// <param name="Id">Identifier of the entry that now holds the weight.</param>
/// <param name="Created">True when a new entry was created, false when the day's existing entry was corrected.</param>
public sealed record LogWeightEntryResult(Guid Id, bool Created);
