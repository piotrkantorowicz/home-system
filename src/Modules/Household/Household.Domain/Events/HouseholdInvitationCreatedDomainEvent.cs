namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when an owner invites someone, by email, by person, or both. No mail is sent in v1; the
/// event exists for auditing and future notification.
/// </summary>
/// <param name="InvitationId">The new invitation.</param>
/// <param name="HouseholdId">The household being joined.</param>
/// <param name="Email">The normalised address invited, or <see langword="null"/> when addressed only by <paramref name="TargetPersonId"/>.</param>
/// <param name="TargetPersonId">The specific person invited, or <see langword="null"/> when addressed only by <paramref name="Email"/>.</param>
/// <param name="Role">The role granted on acceptance.</param>
public sealed record HouseholdInvitationCreatedDomainEvent(
    HouseholdInvitationId InvitationId,
    HouseholdId HouseholdId,
    string? Email,
    PersonId? TargetPersonId,
    HouseholdRole Role) : IDomainEvent;
