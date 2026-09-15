namespace Household.UnitTests.Application;

using Household.Application.Commands.ConvertManagedMemberToAccount;
using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>Unit tests for <c>ConvertManagedMemberToAccountCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class ConvertManagedMemberToAccountCommandHandlerTests
{
    private readonly IPersonRepository _persons = Substitute.For<IPersonRepository>();
    private readonly IHouseholdRepository _households = Substitute.For<IHouseholdRepository>();
    private readonly IHouseholdUnitOfWork _uow = Substitute.For<IHouseholdUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly ConvertManagedMemberToAccountCommandHandler _sut;

    private readonly Person _owner = Person.RegisterFromLogin(PersonId.New(), "auth|owner", "Owner", null, null, TestClock.UtcNow);
    private readonly HouseholdAggregate _household;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public ConvertManagedMemberToAccountCommandHandlerTests()
    {
        _household = HouseholdAggregate.Create(HouseholdId.New(), "Home", _owner.Id, TestClock.UtcNow);
        _sut = new ConvertManagedMemberToAccountCommandHandler(
            new HouseholdAccessService(_persons, _households), _persons, _uow, _clock);

        _persons.GetByAuthSubjectAsync("auth|owner", Arg.Any<CancellationToken>()).Returns(_owner);
        _households.GetByIdAsync(_household.Id, Arg.Any<CancellationToken>()).Returns(_household);
    }

    private ConvertManagedMemberToAccountCommand Command(Guid personId, string email = "kiddo@x.com")
        => new("auth|owner", _household.Id.Value, personId, email);

    /// <summary><c>Handle</c> marks the pending link and commits.</summary>
    [Fact]
    public async Task Handle_MarksThePendingLink_AndCommits()
    {
        var managed = Person.CreateManaged(PersonId.New(), "Kiddo", null, TestClock.UtcNow);
        _household.AddMember(managed.Id, HouseholdRole.Child, TestClock.UtcNow);
        _persons.GetByIdAsync(managed.Id, Arg.Any<CancellationToken>()).Returns(managed);

        await _sut.HandleAsync(Command(managed.Id.Value), CancellationToken.None);

        managed.Email!.Value.ShouldBe("kiddo@x.com");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When person is not a member of the household: <c>Handle</c> throws.</summary>
    [Fact]
    public async Task Handle_WhenPersonIsNotAMemberOfTheHousehold_Throws()
    {
        var stranger = Person.CreateManaged(PersonId.New(), "Stranger", null, TestClock.UtcNow);
        _persons.GetByIdAsync(stranger.Id, Arg.Any<CancellationToken>()).Returns(stranger);

        await Should.ThrowAsync<ForbiddenException>(() =>
            _sut.HandleAsync(Command(stranger.Id.Value), CancellationToken.None));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When person is already linked: <c>Handle</c> throws.</summary>
    [Fact]
    public async Task Handle_WhenPersonIsAlreadyLinked_Throws()
    {
        var linked = Person.RegisterFromLogin(PersonId.New(), "auth|kid", "Kid", null, null, TestClock.UtcNow);
        _household.AddMember(linked.Id, HouseholdRole.Adult, TestClock.UtcNow);
        _persons.GetByIdAsync(linked.Id, Arg.Any<CancellationToken>()).Returns(linked);

        await Should.ThrowAsync<HouseholdDomainException>(() =>
            _sut.HandleAsync(Command(linked.Id.Value), CancellationToken.None));
    }

    /// <summary>When caller is not owner: <c>Handle</c> throws.</summary>
    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_Throws()
    {
        var adult = Person.RegisterFromLogin(PersonId.New(), "auth|adult", "Adult", null, null, TestClock.UtcNow);
        _household.AddMember(adult.Id, HouseholdRole.Adult, TestClock.UtcNow);
        _persons.GetByAuthSubjectAsync("auth|adult", Arg.Any<CancellationToken>()).Returns(adult);

        var managed = Person.CreateManaged(PersonId.New(), "Kiddo", null, TestClock.UtcNow);
        _household.AddMember(managed.Id, HouseholdRole.Child, TestClock.UtcNow);
        _persons.GetByIdAsync(managed.Id, Arg.Any<CancellationToken>()).Returns(managed);

        await Should.ThrowAsync<ForbiddenException>(() => _sut.HandleAsync(
            new ConvertManagedMemberToAccountCommand(
                "auth|adult", _household.Id.Value, managed.Id.Value, "k@x.com"),
            CancellationToken.None));
    }
}
