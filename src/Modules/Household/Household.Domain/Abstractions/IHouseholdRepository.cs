namespace Household.Domain.Abstractions;

using Household.Domain.ValueObjects;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>
/// Write-side access to household aggregates, always loaded with their members. Queries bypass
/// this and read the DbContext directly.
/// </summary>
public interface IHouseholdRepository
{
    /// <summary>Loads a household by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked household with its members, or <see langword="null"/> when it does not exist.</returns>
    Task<HouseholdAggregate?> GetByIdAsync(HouseholdId id, CancellationToken ct = default);

    /// <summary>The household the person currently belongs to, or <c>null</c>. Includes members.</summary>
    /// <param name="personId">The person whose membership is looked up.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<HouseholdAggregate?> GetByMemberPersonIdAsync(PersonId personId, CancellationToken ct = default);

    /// <summary>Stages a new household; it is written when the unit of work commits.</summary>
    /// <param name="household">The household to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(HouseholdAggregate household, CancellationToken ct = default);

    /// <summary>Marks a household and its memberships for deletion; the write happens on commit.</summary>
    /// <param name="household">The tracked household.</param>
    void Remove(HouseholdAggregate household);
}
