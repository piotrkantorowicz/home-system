namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed record HouseholdInvitationCreatedDomainEvent(
    HouseholdInvitationId InvitationId,
    HouseholdId HouseholdId,
    string Email,
    HouseholdRole Role) : IDomainEvent;
