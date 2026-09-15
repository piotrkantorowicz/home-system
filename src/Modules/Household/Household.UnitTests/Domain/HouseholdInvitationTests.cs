namespace Household.UnitTests.Domain;

using Household.Domain.Aggregates;
using Household.Domain.Events;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;

/// <summary>Unit tests for <c>HouseholdInvitation</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class HouseholdInvitationTests
{
    private static HouseholdInvitation NewInvitation(HouseholdRole role = HouseholdRole.Adult)
        => HouseholdInvitation.Create(
            HouseholdInvitationId.New(),
            HouseholdId.New(),
            PersonEmail.Create("invitee@example.com"),
            role,
            PersonId.New(),
            TestClock.UtcNow);

    /// <summary><c>Create</c> is pending and expires in 30 days and raises created event.</summary>
    [Fact]
    public void Create_IsPending_ExpiresIn30Days_AndRaisesCreatedEvent()
    {
        var invitation = NewInvitation();

        invitation.IsPending.ShouldBeTrue();
        invitation.ExpiresAt.ShouldBe(invitation.CreatedAt.Add(HouseholdInvitation.Lifetime), tolerance: TimeSpan.FromSeconds(1));
        invitation.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<HouseholdInvitationCreatedDomainEvent>();
    }

    /// <summary>For owner role: <c>Create</c> throws.</summary>
    [Fact]
    public void Create_ForOwnerRole_Throws()
        => Should.Throw<HouseholdDomainException>(() => NewInvitation(HouseholdRole.Owner));

    /// <summary>When pending and fresh: <c>Accept</c> marks accepted.</summary>
    [Fact]
    public void Accept_WhenPendingAndFresh_MarksAccepted()
    {
        var invitation = NewInvitation();

        invitation.Accept(TestClock.UtcNow.AddMinutes(1));

        invitation.Status.ShouldBe(InvitationStatus.Accepted);
        invitation.ResolvedAt.ShouldNotBeNull();
    }

    /// <summary>When expired: <c>Accept</c> marks expired and throws.</summary>
    [Fact]
    public void Accept_WhenExpired_MarksExpired_AndThrows()
    {
        var invitation = NewInvitation();

        Should.Throw<HouseholdDomainException>(
            () => invitation.Accept(invitation.ExpiresAt.AddSeconds(1)));

        invitation.Status.ShouldBe(InvitationStatus.Expired);
    }

    /// <summary>When pending: <c>Revoke</c> marks revoked.</summary>
    [Fact]
    public void Revoke_WhenPending_MarksRevoked()
    {
        var invitation = NewInvitation();

        invitation.Revoke(TestClock.UtcNow.AddMinutes(1));

        invitation.Status.ShouldBe(InvitationStatus.Revoked);
    }

    /// <summary>After revoke: <c>Accept</c> throws.</summary>
    [Fact]
    public void Accept_AfterRevoke_Throws()
    {
        var invitation = NewInvitation();
        invitation.Revoke(TestClock.UtcNow.AddMinutes(1));

        Should.Throw<HouseholdDomainException>(() => invitation.Accept(TestClock.UtcNow.AddMinutes(1)));
    }
}
