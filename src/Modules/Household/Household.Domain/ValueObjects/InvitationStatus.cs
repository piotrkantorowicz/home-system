namespace Household.Domain.ValueObjects;

/// <summary>
/// Lifecycle of a <c>HouseholdInvitation</c>. Only a <see cref="Pending"/> invitation can change
/// state; the other three are terminal.
/// </summary>
public enum InvitationStatus
{
    /// <summary>Waiting for a matching account to sign in.</summary>
    Pending,
    /// <summary>The invitee signed in and joined the household.</summary>
    Accepted,
    /// <summary>An owner withdrew it before it was accepted.</summary>
    Revoked,
    /// <summary>The invitee declined it.</summary>
    Declined,
    /// <summary>Its 30-day lifetime passed without acceptance.</summary>
    Expired,
}
