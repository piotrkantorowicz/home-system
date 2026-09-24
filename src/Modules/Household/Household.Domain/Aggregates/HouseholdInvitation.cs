namespace Household.Domain.Aggregates;

using global::Household.Domain.Events;
using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A pending household membership, addressed by email, by a known <see cref="Person"/>, or both.
/// An email-only invitation (someone who has never signed in) resolves by matching login email; a
/// person-targeted invitation (an existing account picked by an owner, which may have no email on
/// file) resolves by that person's identity. In v1 no email is sent — an owner tells the invitee
/// verbally to sign in. Signing in alone does not join the household: the invitee must explicitly
/// accept or decline (see <c>AcceptInvitationCommand</c> / <c>DeclineInvitationCommand</c>).
/// </summary>
public sealed class HouseholdInvitation : AggregateRoot<HouseholdInvitationId>
{
    /// <summary>How long an invitation stays pending before it expires: 30 days.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private HouseholdInvitation() { }

    /// <summary>Creates a pending invitation and raises <see cref="HouseholdInvitationCreatedDomainEvent"/>.</summary>
    /// <param name="id">Identifier for the new invitation.</param>
    /// <param name="householdId">The household being joined.</param>
    /// <param name="email">The address the invitee's login must match; may be <see langword="null"/> when <paramref name="targetPersonId"/> is given.</param>
    /// <param name="targetPersonId">The specific person addressed, when already known; may be <see langword="null"/> when <paramref name="email"/> is given.</param>
    /// <param name="role">The role granted on acceptance; cannot be <see cref="HouseholdRole.Owner"/>.</param>
    /// <param name="invitedByPersonId">The owner who issued it.</param>
    /// <exception cref="HouseholdDomainException">Neither <paramref name="email"/> nor <paramref name="targetPersonId"/> is given, or <paramref name="role"/> is owner.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <param name="nickname">Optional household-local nickname, applied when the invitation is accepted.</param>
    public static HouseholdInvitation Create(
        HouseholdInvitationId id,
        HouseholdId householdId,
        PersonEmail? email,
        PersonId? targetPersonId,
        HouseholdRole role,
        PersonId invitedByPersonId,
        DateTime now,
        string? nickname = null)
    {
        if (email is null && targetPersonId is null)
            throw new HouseholdDomainException("An invitation needs either an email address or a person to address.");

        if (role == HouseholdRole.Owner)
            throw new HouseholdDomainException("An invitation cannot grant the owner role.");

        var invitation = new HouseholdInvitation
        {
            Id = id,
            HouseholdId = householdId,
            Email = email,
            TargetPersonId = targetPersonId,
            Role = role,
            Nickname = nickname,
            InvitedByPersonId = invitedByPersonId,
            Status = InvitationStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        };

        invitation.RaiseDomainEvent(new HouseholdInvitationCreatedDomainEvent(
            id, householdId, email?.Value, targetPersonId, role));

        return invitation;
    }

    /// <summary>The household being joined.</summary>
    public HouseholdId HouseholdId { get; private set; } = default!;
    /// <summary>The address the invitee's login must match, or <see langword="null"/> when addressed only by <see cref="TargetPersonId"/>.</summary>
    public PersonEmail? Email { get; private set; }
    /// <summary>The specific person addressed, or <see langword="null"/> when addressed only by <see cref="Email"/>.</summary>
    public PersonId? TargetPersonId { get; private set; }
    /// <summary>The role granted on acceptance; never owner.</summary>
    public HouseholdRole Role { get; private set; }
    /// <summary>The owner who issued the invitation.</summary>
    public PersonId InvitedByPersonId { get; private set; } = default!;
    /// <summary>Household-local nickname to apply on acceptance, or <see langword="null"/>.</summary>
    public string? Nickname { get; private set; }
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

    /// <summary>Whether this invitation addresses the given person — by <see cref="TargetPersonId"/> or by a matching <see cref="Email"/>.</summary>
    /// <param name="personId">The person to check.</param>
    /// <param name="email">That person's own email, if any.</param>
    public bool IsAddressedTo(PersonId personId, PersonEmail? email)
        => TargetPersonId == personId || (Email is not null && email is not null && Email == email);

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

    /// <summary>The invitee turns down a pending invitation.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    /// <exception cref="HouseholdDomainException">The invitation is not pending.</exception>
    public void Decline(DateTime utcNow)
    {
        EnsurePending();
        Status = InvitationStatus.Declined;
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
