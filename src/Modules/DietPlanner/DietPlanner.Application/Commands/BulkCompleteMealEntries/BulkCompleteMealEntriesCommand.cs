namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using Shared.Abstractions.CQRS;

public sealed record BulkCompleteMealEntriesCommand(string UserId, DateOnly Date)
    : ICommand<BulkCompleteResult>;

public sealed record BulkCompleteResult(int Completed);
