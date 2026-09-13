namespace Household.Domain.Abstractions;

using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;

public interface IHouseholdInvitationRepository
{
    Task<HouseholdInvitation?> GetByIdAsync(HouseholdInvitationId id, CancellationToken ct = default);

    /// <summary>The single pending invitation for this email, if any.</summary>
    Task<HouseholdInvitation?> GetPendingByEmailAsync(PersonEmail email, CancellationToken ct = default);

    Task<IReadOnlyList<HouseholdInvitation>> ListForHouseholdAsync(
        HouseholdId householdId, CancellationToken ct = default);

    Task<bool> HasPendingForEmailInHouseholdAsync(
        HouseholdId householdId, PersonEmail email, CancellationToken ct = default);

    Task AddAsync(HouseholdInvitation invitation, CancellationToken ct = default);
}
