namespace DietPlanner.Application.Queries.GetWeightEntries;

using Shared.Abstractions.CQRS;

public sealed record GetWeightEntriesQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<WeightEntryDto>>;
