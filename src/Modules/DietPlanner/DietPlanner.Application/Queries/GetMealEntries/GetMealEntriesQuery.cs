namespace DietPlanner.Application.Queries.GetMealEntries;

using Shared.Abstractions.CQRS;

public sealed record GetMealEntriesQuery(
    string UserId,
    DateOnly? From,
    DateOnly? To) : IQuery<IReadOnlyList<MealEntryDto>>;
