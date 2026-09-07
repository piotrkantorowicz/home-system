using Household.Domain.Events;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

namespace Household.Domain.Aggregates;

/// <summary>
/// A pending household membership addressed by email. In v1 no email is sent — the
/// invitation resolves the next time someone signs in whose account email matches
/// (see the login sync handler). An owner tells the invitee verbally to log in once.
/// </summary>
public sealed class HouseholdInvitation : AggregateRoot<HouseholdInvitationId>
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private HouseholdInvitation() { }

    public static HouseholdInvitation Create(
        HouseholdInvitationId id,
        HouseholdId householdId,
        PersonEmail email,
        HouseholdRole role,
        PersonId invitedByPersonId)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (role == HouseholdRole.Owner)
            throw new HouseholdDomainException("An invitation cannot grant the owner role.");

        var now = DateTime.UtcNow;
        var invitation = new HouseholdInvitation
        {
            Id = id,
            HouseholdId = householdId,
            Email = email,
            Role = role,
            InvitedByPersonId = invitedByPersonId,
            Status = InvitationStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        };

        invitation.RaiseDomainEvent(new HouseholdInvitationCreatedDomainEvent(
            id, householdId, email.Value, role));

        return invitation;
    }

    public HouseholdId HouseholdId { get; private set; } = default!;
    public PersonEmail Email { get; private set; } = default!;
    public HouseholdRole Role { get; private set; }
    public PersonId InvitedByPersonId { get; private set; } = default!;
    public InvitationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public bool IsPending => Status == InvitationStatus.Pending;

    public bool HasExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public void Accept(DateTime utcNow)
    {
        EnsurePending();

        if (HasExpired(utcNow))
        {
            Expire(utcNow);
            throw new HouseholdDomainException("This invitation has expired.");
        }

        Status = InvitationStatus.Accepted;
        ResolvedAt = utcNow;
    }

    public void Revoke(DateTime utcNow)
    {
        EnsurePending();
        Status = InvitationStatus.Revoked;
        ResolvedAt = utcNow;
    }

    public void Expire(DateTime utcNow)
    {
        if (!IsPending)
            return;

        Status = InvitationStatus.Expired;
        ResolvedAt = utcNow;
    }

    private void EnsurePending()
    {
        if (!IsPending)
            throw new HouseholdDomainException($"This invitation is already {Status.ToString().ToLowerInvariant()}.");
    }
}
