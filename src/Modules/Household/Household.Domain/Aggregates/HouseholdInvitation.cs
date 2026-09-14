namespace Household.Domain.Aggregates;

using global::Household.Domain.Events;
using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A pending household membership addressed by email. In v1 no email is sent — the
/// invitation resolves the next time someone signs in whose account email matches
/// (see the login sync handler). An owner tells the invitee verbally to log in once.
/// </summary>
public sealed class HouseholdInvitation : AggregateRoot<HouseholdInvitationId>
{
    /// <summary>How long an invitation stays pending before it expires: 30 days.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private HouseholdInvitation() { }

    /// <summary>Creates a pending invitation and raises <see cref="HouseholdInvitationCreatedDomainEvent"/>.</summary>
    /// <param name="id">Identifier for the new invitation.</param>
    /// <param name="householdId">The household being joined.</param>
    /// <param name="email">The address the invitee's login must match.</param>
    /// <param name="role">The role granted on acceptance; cannot be <see cref="HouseholdRole.Owner"/>.</param>
    /// <param name="invitedByPersonId">The owner who issued it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="email"/> is null.</exception>
    /// <exception cref="HouseholdDomainException"><paramref name="role"/> is owner.</exception>
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

    /// <summary>The household being joined.</summary>
    public HouseholdId HouseholdId { get; private set; } = default!;
    /// <summary>The address the invitee's login must match.</summary>
    public PersonEmail Email { get; private set; } = default!;
    /// <summary>The role granted on acceptance; never owner.</summary>
    public HouseholdRole Role { get; private set; }
    /// <summary>The owner who issued the invitation.</summary>
    public PersonId InvitedByPersonId { get; private set; } = default!;
    /// <summary>Where the invitation is in its lifecycle.</summary>
    public InvitationStatus Status { get; private set; }
    /// <summary>When it was issued, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>When it stops being acceptable: <see cref="CreatedAt"/> plus <see cref="Lifetime"/>.</summary>
    public DateTime ExpiresAt { get; private set; }
    /// <summary>When it left the pending state, UTC; <see langword="null"/> while pending.</summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>Whether the invitation can still be accepted or revoked.</summary>
    public bool IsPending => Status == InvitationStatus.Pending;

    /// <summary>Whether the lifetime has passed at the given instant, regardless of <see cref="Status"/>.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    public bool HasExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    /// <summary>Marks the invitation accepted. An expired-but-pending invitation is expired instead and the call fails.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    /// <exception cref="HouseholdDomainException">The invitation is not pending, or has expired.</exception>
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

    /// <summary>Withdraws a pending invitation.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    /// <exception cref="HouseholdDomainException">The invitation is not pending.</exception>
    public void Revoke(DateTime utcNow)
    {
        EnsurePending();
        Status = InvitationStatus.Revoked;
        ResolvedAt = utcNow;
    }

    /// <summary>Marks a pending invitation expired; a no-op for any other status.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
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
