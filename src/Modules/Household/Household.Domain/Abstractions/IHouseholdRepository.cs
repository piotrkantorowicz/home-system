namespace Household.Domain.Abstractions;

using Household.Domain.ValueObjects;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

public interface IHouseholdRepository
{
    Task<HouseholdAggregate?> GetByIdAsync(HouseholdId id, CancellationToken ct = default);

    /// <summary>The household the person currently belongs to, or <c>null</c>. Includes members.</summary>
    Task<HouseholdAggregate?> GetByMemberPersonIdAsync(PersonId personId, CancellationToken ct = default);

    Task AddAsync(HouseholdAggregate household, CancellationToken ct = default);

    void Remove(HouseholdAggregate household);
}
