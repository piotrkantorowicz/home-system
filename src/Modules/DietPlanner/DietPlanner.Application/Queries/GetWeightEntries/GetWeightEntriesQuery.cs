namespace DietPlanner.Application.Queries.GetWeightEntries;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Lists the caller's weigh-ins in a date range, oldest first.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="From">First day to include, or <see langword="null"/> for no lower bound.</param>
/// <param name="To">Last day to include, or <see langword="null"/> for no upper bound.</param>
public sealed record GetWeightEntriesQuery(
    Guid PersonId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<WeightEntryDto>>;
