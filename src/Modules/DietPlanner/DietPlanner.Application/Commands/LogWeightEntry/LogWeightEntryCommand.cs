namespace DietPlanner.Application.Commands.LogWeightEntry;

using Shared.Abstractions.Cqrs;

public sealed record LogWeightEntryCommand(
    string UserId,
    DateOnly Date,
    decimal WeightKg) : ICommand<LogWeightEntryResult>;

public sealed record LogWeightEntryResult(Guid Id, bool Created);
