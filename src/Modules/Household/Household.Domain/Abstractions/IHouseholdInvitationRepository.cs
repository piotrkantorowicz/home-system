namespace Household.Domain.Abstractions;

using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="HouseholdInvitation"/> aggregates. Queries bypass this and read
/// the DbContext directly; invitations are never deleted, only resolved.
/// </summary>
public interface IHouseholdInvitationRepository
{
    /// <summary>Loads an invitation by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked invitation, or <see langword="null"/> when it does not exist.</returns>
    Task<HouseholdInvitation?> GetByIdAsync(HouseholdInvitationId id, CancellationToken ct = default);

    /// <summary>Loads every invitation a household ever issued, in any status.</summary>
    /// <param name="householdId">The household.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<IReadOnlyList<HouseholdInvitation>> ListForHouseholdAsync(
        HouseholdId householdId, CancellationToken ct = default);

    /// <summary>Whether the household already has a pending, unexpired invitation for the address — used to reject duplicates.</summary>
    /// <param name="householdId">The household.</param>
    /// <param name="email">The normalised address.</param>
    /// <param name="now">Current time, UTC; an invitation whose lifetime has passed does not count.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<bool> HasPendingForEmailInHouseholdAsync(
        HouseholdId householdId, PersonEmail email, DateTime now, CancellationToken ct = default);

    /// <summary>Whether the household already has a pending, unexpired invitation targeting this person — used to reject duplicates.</summary>
    /// <param name="householdId">The household.</param>
    /// <param name="personId">The targeted person.</param>
    /// <param name="now">Current time, UTC; an invitation whose lifetime has passed does not count.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<bool> HasPendingForPersonInHouseholdAsync(
        HouseholdId householdId, PersonId personId, DateTime now, CancellationToken ct = default);

    /// <summary>Stages a new invitation; it is written when the unit of work commits.</summary>
    /// <param name="invitation">The invitation to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(HouseholdInvitation invitation, CancellationToken ct = default);
}
