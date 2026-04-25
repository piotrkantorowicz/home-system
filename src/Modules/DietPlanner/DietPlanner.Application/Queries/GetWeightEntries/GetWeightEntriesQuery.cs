namespace DietPlanner.Application.Queries.GetWeightEntries;

using Shared.Abstractions.Cqrs;

public sealed record GetWeightEntriesQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<WeightEntryDto>>;
