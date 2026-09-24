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
            targetPersonId: null,
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

    /// <summary>When pending: <c>Decline</c> marks declined.</summary>
    [Fact]
    public void Decline_WhenPending_MarksDeclined()
    {
        var invitation = NewInvitation();

        invitation.Decline(TestClock.UtcNow.AddMinutes(1));

        invitation.Status.ShouldBe(InvitationStatus.Declined);
        invitation.ResolvedAt.ShouldNotBeNull();
    }

    /// <summary>After decline: <c>Accept</c> throws.</summary>
    [Fact]
    public void Accept_AfterDecline_Throws()
    {
        var invitation = NewInvitation();
        invitation.Decline(TestClock.UtcNow.AddMinutes(1));

        Should.Throw<HouseholdDomainException>(() => invitation.Accept(TestClock.UtcNow.AddMinutes(1)));
    }

    /// <summary>When already accepted: <c>Decline</c> throws.</summary>
    [Fact]
    public void Decline_AfterAccept_Throws()
    {
        var invitation = NewInvitation();
        invitation.Accept(TestClock.UtcNow.AddMinutes(1));

        Should.Throw<HouseholdDomainException>(() => invitation.Decline(TestClock.UtcNow.AddMinutes(1)));
    }

    /// <summary><c>Create</c> carries the nickname through for use when the invitation is accepted.</summary>
    [Fact]
    public void Create_WithNickname_CarriesItThrough()
    {
        var invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(),
            HouseholdId.New(),
            PersonEmail.Create("invitee@example.com"),
            targetPersonId: null,
            HouseholdRole.Adult,
            PersonId.New(),
            TestClock.UtcNow,
            nickname: "Gram");

        invitation.Nickname.ShouldBe("Gram");
    }

    /// <summary>Addressed only by target person, with no email: <c>Create</c> succeeds.</summary>
    [Fact]
    public void Create_WithTargetPersonAndNoEmail_Succeeds()
    {
        var target = PersonId.New();

        var invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(),
            HouseholdId.New(),
            email: null,
            targetPersonId: target,
            HouseholdRole.Adult,
            PersonId.New(),
            TestClock.UtcNow);

        invitation.Email.ShouldBeNull();
        invitation.TargetPersonId.ShouldBe(target);
        invitation.IsPending.ShouldBeTrue();
    }

    /// <summary>Neither email nor target person given: <c>Create</c> throws.</summary>
    [Fact]
    public void Create_WithNeitherEmailNorTargetPerson_Throws()
        => Should.Throw<HouseholdDomainException>(() => HouseholdInvitation.Create(
            HouseholdInvitationId.New(),
            HouseholdId.New(),
            email: null,
            targetPersonId: null,
            HouseholdRole.Adult,
            PersonId.New(),
            TestClock.UtcNow));

    /// <summary><c>IsAddressedTo</c> matches by target person id even without a matching email.</summary>
    [Fact]
    public void IsAddressedTo_MatchesByTargetPersonId()
    {
        var target = PersonId.New();
        var invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(),
            HouseholdId.New(),
            email: null,
            targetPersonId: target,
            HouseholdRole.Adult,
            PersonId.New(),
            TestClock.UtcNow);

        invitation.IsAddressedTo(target, email: null).ShouldBeTrue();
        invitation.IsAddressedTo(PersonId.New(), email: null).ShouldBeFalse();
    }

    /// <summary><c>IsAddressedTo</c> matches by email when there is no target person.</summary>
    [Fact]
    public void IsAddressedTo_MatchesByEmail_WhenNoTargetPerson()
    {
        var invitation = NewInvitation();
        var matchingEmail = PersonEmail.Create("invitee@example.com");
        var otherEmail = PersonEmail.Create("someone-else@example.com");

        invitation.IsAddressedTo(PersonId.New(), matchingEmail).ShouldBeTrue();
        invitation.IsAddressedTo(PersonId.New(), otherEmail).ShouldBeFalse();
        invitation.IsAddressedTo(PersonId.New(), email: null).ShouldBeFalse();
    }
}
