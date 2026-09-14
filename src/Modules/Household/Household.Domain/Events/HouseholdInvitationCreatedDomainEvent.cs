namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when an owner invites someone by email. No mail is sent in v1; the event exists for auditing and future notification.
/// </summary>
/// <param name="InvitationId">The new invitation.</param>
/// <param name="HouseholdId">The household being joined.</param>
/// <param name="Email">The normalised address invited.</param>
/// <param name="Role">The role granted on acceptance.</param>
public sealed record HouseholdInvitationCreatedDomainEvent(
    HouseholdInvitationId InvitationId,
    HouseholdId HouseholdId,
    string Email,
    HouseholdRole Role) : IDomainEvent;
