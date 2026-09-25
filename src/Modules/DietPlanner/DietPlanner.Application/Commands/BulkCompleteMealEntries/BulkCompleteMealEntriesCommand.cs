namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks every still-planned meal of the caller on one day as done; entries already done or modified are left alone.
/// </summary>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="Date">The calendar day.</param>
public sealed record BulkCompleteMealEntriesCommand(Guid PersonId, DateOnly Date)
    : ICommand<BulkCompleteResult>;

/// <summary>
/// Outcome of a bulk completion.
/// </summary>
/// <param name="Completed">How many entries changed from planned to done.</param>
public sealed record BulkCompleteResult(int Completed);
