namespace Household.UnitTests.Application;

using Household.Application.Commands.DeclineInvitation;
using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>DeclineInvitationCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class DeclineInvitationCommandHandlerTests
{
    private readonly IPersonRepository _persons = Substitute.For<IPersonRepository>();
    private readonly IHouseholdRepository _households = Substitute.For<IHouseholdRepository>();
    private readonly IHouseholdInvitationRepository _invitations = Substitute.For<IHouseholdInvitationRepository>();
    private readonly IHouseholdUnitOfWork _uow = Substitute.For<IHouseholdUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly DeclineInvitationCommandHandler _sut;

    private readonly Person _invitee = Person.RegisterFromLogin(
        PersonId.New(), "auth|invitee", "Invitee", PersonEmail.Create("invitee@example.com"), null, TestClock.UtcNow);
    private readonly HouseholdInvitation _invitation;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public DeclineInvitationCommandHandlerTests()
    {
        _invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(), HouseholdId.New(), PersonEmail.Create("invitee@example.com"),
            targetPersonId: null, HouseholdRole.Adult, PersonId.New(), TestClock.UtcNow);

        _sut = new DeclineInvitationCommandHandler(
            new HouseholdAccessService(_persons, _households), _invitations, _uow, _clock);

        _persons.GetByAuthSubjectAsync("auth|invitee", Arg.Any<CancellationToken>()).Returns(_invitee);
        _invitations.GetByIdAsync(_invitation.Id, Arg.Any<CancellationToken>()).Returns(_invitation);
    }

    /// <summary>When pending and addressed to the caller: <c>Handle</c> declines and commits.</summary>
    [Fact]
    public async Task Handle_WhenPendingAndAddressedToCaller_Declines_AndCommits()
    {
        await _sut.HandleAsync(
            new DeclineInvitationCommand("auth|invitee", _invitation.Id.Value), TestContext.Current.CancellationToken);

        _invitation.Status.ShouldBe(InvitationStatus.Declined);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the invitation is addressed to a different email: <c>Handle</c> throws forbidden.</summary>
    [Fact]
    public async Task Handle_WhenAddressedToADifferentEmail_Throws()
    {
        var stranger = Person.RegisterFromLogin(
            PersonId.New(), "auth|stranger", "Stranger", PersonEmail.Create("stranger@example.com"), null, TestClock.UtcNow);
        _persons.GetByAuthSubjectAsync("auth|stranger", Arg.Any<CancellationToken>()).Returns(stranger);

        await Should.ThrowAsync<ForbiddenException>(() => _sut.HandleAsync(
            new DeclineInvitationCommand("auth|stranger", _invitation.Id.Value), TestContext.Current.CancellationToken));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When targeted at a specific person: <c>Handle</c> throws for a different account sharing that person's email, even though email alone would otherwise match.</summary>
    [Fact]
    public async Task Handle_WhenTargetedAtAPerson_ThrowsForADifferentAccountWithTheSameEmail()
    {
        var target = Person.RegisterFromLogin(
            PersonId.New(), "auth|target", "Target", PersonEmail.Create("shared@example.com"), null, TestClock.UtcNow);
        var targeted = HouseholdInvitation.Create(
            HouseholdInvitationId.New(), HouseholdId.New(), PersonEmail.Create("shared@example.com"),
            targetPersonId: target.Id, HouseholdRole.Adult, PersonId.New(), TestClock.UtcNow);
        _invitations.GetByIdAsync(targeted.Id, Arg.Any<CancellationToken>()).Returns(targeted);

        var impersonator = Person.RegisterFromLogin(
            PersonId.New(), "auth|impersonator", "Impersonator", PersonEmail.Create("shared@example.com"), null, TestClock.UtcNow);
        _persons.GetByAuthSubjectAsync("auth|impersonator", Arg.Any<CancellationToken>()).Returns(impersonator);

        await Should.ThrowAsync<ForbiddenException>(() => _sut.HandleAsync(
            new DeclineInvitationCommand("auth|impersonator", targeted.Id.Value), TestContext.Current.CancellationToken));

        targeted.Status.ShouldBe(InvitationStatus.Pending);
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When no invitation exists for the id: <c>Handle</c> throws not found.</summary>
    [Fact]
    public async Task Handle_WhenInvitationDoesNotExist_Throws()
    {
        _invitations.GetByIdAsync(Arg.Any<HouseholdInvitationId>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdInvitation?)null);

        await Should.ThrowAsync<NotFoundException>(() => _sut.HandleAsync(
            new DeclineInvitationCommand("auth|invitee", _invitation.Id.Value), TestContext.Current.CancellationToken));
    }
}
