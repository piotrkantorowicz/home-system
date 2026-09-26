namespace DietPlanner.Application.Queries.GetMealEntries;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Lists one household member's meal entries in a date range with their resolved recipe, macros and completion state, ordered by date then slot.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="From">First day to include, or <see langword="null"/> for no lower bound.</param>
/// <param name="To">Last day to include, or <see langword="null"/> for no upper bound.</param>
/// <param name="AuthSubject">Auth subject of the caller; resolves their household.</param>
/// <param name="ForPersonId">Household member whose meals to list; the caller when <see langword="null"/>.</param>
public sealed record GetMealEntriesQuery(
    Guid PersonId,
    DateOnly? From,
    DateOnly? To,
    string AuthSubject,
    Guid? ForPersonId = null) : IQuery<IReadOnlyList<MealEntryDto>>;
