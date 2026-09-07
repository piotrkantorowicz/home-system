using Household.Application.Commands.CreateHousehold;
using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

namespace Household.UnitTests.Application;

public sealed class CreateHouseholdCommandHandlerTests
{
    private readonly IPersonRepository _persons = Substitute.For<IPersonRepository>();
    private readonly IHouseholdRepository _households = Substitute.For<IHouseholdRepository>();
    private readonly IHouseholdUnitOfWork _uow = Substitute.For<IHouseholdUnitOfWork>();
    private readonly CreateHouseholdCommandHandler _sut;

    public CreateHouseholdCommandHandlerTests()
        => _sut = new CreateHouseholdCommandHandler(
            new HouseholdAccessService(_persons, _households), _households, _uow);

    private Person GivenCaller()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "auth|1", "Caller", null, null);
        _persons.GetByAuthSubjectAsync("auth|1", Arg.Any<CancellationToken>()).Returns(person);
        return person;
    }

    [Fact]
    public async Task Handle_CreatesHousehold_WithTheCallerAsOwner_AndCommits()
    {
        GivenCaller();
        _households.GetByMemberPersonIdAsync(Arg.Any<PersonId>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdAggregate?)null);

        var id = await _sut.HandleAsync(
            new CreateHouseholdCommand("auth|1", "New Home"), CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _households.Received(1).AddAsync(
            Arg.Is<HouseholdAggregate>(h => h.Name == "New Home" && h.Members.Count == 1),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCallerAlreadyInAHousehold_Throws_AndDoesNotCommit()
    {
        var caller = GivenCaller();
        _households.GetByMemberPersonIdAsync(caller.Id, Arg.Any<CancellationToken>())
            .Returns(HouseholdAggregate.Create(HouseholdId.New(), "Existing", caller.Id));

        await Should.ThrowAsync<HouseholdDomainException>(() =>
            _sut.HandleAsync(new CreateHouseholdCommand("auth|1", "Another"), CancellationToken.None));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoPersonForSubject_ThrowsNotFound()
    {
        _persons.GetByAuthSubjectAsync("auth|ghost", Arg.Any<CancellationToken>()).Returns((Person?)null);

        await Should.ThrowAsync<Shared.Abstractions.Core.Domain.NotFoundException>(() =>
            _sut.HandleAsync(new CreateHouseholdCommand("auth|ghost", "X"), CancellationToken.None));
    }
}
