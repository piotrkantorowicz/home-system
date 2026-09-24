namespace Household.UnitTests.Application;

using Household.Application.Commands.CreateHousehold;
using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>Unit tests for <c>CreateHouseholdCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class CreateHouseholdCommandHandlerTests
{
    private readonly IPersonRepository _persons = Substitute.For<IPersonRepository>();
    private readonly IHouseholdRepository _households = Substitute.For<IHouseholdRepository>();
    private readonly IHouseholdUnitOfWork _uow = Substitute.For<IHouseholdUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly CreateHouseholdCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public CreateHouseholdCommandHandlerTests()
        => _sut = new CreateHouseholdCommandHandler(
            new HouseholdAccessService(_persons, _households), _households, _uow, _clock);

    private Person GivenCaller()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "auth|1", "Caller", null, null, TestClock.UtcNow);
        _persons.GetByAuthSubjectAsync("auth|1", Arg.Any<CancellationToken>()).Returns(person);
        return person;
    }

    /// <summary>Creates household, with the caller as owner: <c>Handle</c> commits.</summary>
    [Fact]
    public async Task Handle_CreatesHousehold_WithTheCallerAsOwner_AndCommits()
    {
        GivenCaller();
        _households.GetByMemberPersonIdAsync(Arg.Any<PersonId>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdAggregate?)null);

        var id = await _sut.HandleAsync(
            new CreateHouseholdCommand("auth|1", "New Home"), TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        await _households.Received(1).AddAsync(
            Arg.Is<HouseholdAggregate>(h => h.Name == "New Home" && h.Members.Count == 1),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When caller already in a household: <c>Handle</c> throws and does not commit.</summary>
    [Fact]
    public async Task Handle_WhenCallerAlreadyInAHousehold_Throws_AndDoesNotCommit()
    {
        var caller = GivenCaller();
        _households.GetByMemberPersonIdAsync(caller.Id, Arg.Any<CancellationToken>())
            .Returns(HouseholdAggregate.Create(HouseholdId.New(), "Existing", caller.Id, TestClock.UtcNow));

        await Should.ThrowAsync<HouseholdDomainException>(() =>
            _sut.HandleAsync(new CreateHouseholdCommand("auth|1", "Another"), TestContext.Current.CancellationToken));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When no person for subject: <c>Handle</c> throws not found.</summary>
    [Fact]
    public async Task Handle_WhenNoPersonForSubject_ThrowsNotFound()
    {
        _persons.GetByAuthSubjectAsync("auth|ghost", Arg.Any<CancellationToken>()).Returns((Person?)null);

        await Should.ThrowAsync<Shared.Abstractions.Core.Domain.NotFoundException>(() =>
            _sut.HandleAsync(new CreateHouseholdCommand("auth|ghost", "X"), TestContext.Current.CancellationToken));
    }
}
