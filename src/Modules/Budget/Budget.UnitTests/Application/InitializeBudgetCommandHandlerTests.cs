namespace Budget.UnitTests.Application;

using global::Budget.Application.Commands.InitializeBudget;
using global::Budget.Application.Common;
using global::Budget.Domain.Abstractions;
using global::Budget.Domain.Aggregates;
using global::Budget.Domain.ValueObjects;
using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;
using BudgetAggregate = global::Budget.Domain.Aggregates.Budget;

/// <summary>Unit tests for <c>InitializeBudgetCommandHandler</c>: Household lookup, repositories and unit of work are substituted.</summary>
public sealed class InitializeBudgetCommandHandlerTests
{
    private static readonly Guid HouseholdId = Guid.CreateVersion7();

    private readonly IHouseholdQueryService _households = Substitute.For<IHouseholdQueryService>();
    private readonly IBudgetRepository _budgets = Substitute.For<IBudgetRepository>();
    private readonly IBudgetAccountRepository _accounts = Substitute.For<IBudgetAccountRepository>();
    private readonly IBudgetUnitOfWork _uow = Substitute.For<IBudgetUnitOfWork>();
    private readonly InitializeBudgetCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public InitializeBudgetCommandHandlerTests()
        => _sut = new InitializeBudgetCommandHandler(
            new BudgetAccessService(_households), _budgets, _accounts, _uow, TestClock.Create());

    private void GivenCaller(string role)
        => _households.GetHouseholdContextForUserAsync("auth|1", Arg.Any<CancellationToken>())
            .Returns(new HouseholdContext(HouseholdId, Guid.CreateVersion7(), role, []));

    private Task<InitializeBudgetResult> Run(string? currency = null)
        => _sut.HandleAsync(new InitializeBudgetCommand("auth|1", currency), TestContext.Current.CancellationToken);

    /// <summary>No existing budget: creates it in PLN with one household envelope and commits once.</summary>
    [Theory]
    [InlineData("Owner")]
    [InlineData("Adult")]
    public async Task Handle_WithoutBudget_CreatesBudgetAndDefaultEnvelope(string role)
    {
        GivenCaller(role);

        var result = await Run();

        result.Created.ShouldBeTrue();
        result.Budget.Currency.ShouldBe("PLN");
        await _budgets.Received(1).LockHouseholdAsync(HouseholdId, Arg.Any<CancellationToken>());
        await _budgets.Received(1).AddAsync(
            Arg.Is<BudgetAggregate>(b => b.HouseholdId == HouseholdId), Arg.Any<CancellationToken>());
        await _accounts.Received(1).AddAsync(
            Arg.Is<BudgetAccount>(a => a.Visibility == AccountVisibility.Household && a.OwnerPersonId == null),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>An existing budget in the same currency is returned untouched.</summary>
    [Fact]
    public async Task Handle_WithExistingSameCurrencyBudget_ReturnsItWithoutWriting()
    {
        GivenCaller("Owner");
        var existing = BudgetAggregate.Create(BudgetId.New(), HouseholdId, BudgetCurrency.EUR, TestClock.UtcNow);
        _budgets.GetByHouseholdAsync(HouseholdId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Run("eur");

        result.Created.ShouldBeFalse();
        result.Budget.Id.ShouldBe(existing.Id.Value);
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>An existing budget in another currency conflicts.</summary>
    [Fact]
    public async Task Handle_WithExistingBudgetInOtherCurrency_ThrowsConflict()
    {
        GivenCaller("Owner");
        _budgets.GetByHouseholdAsync(HouseholdId, Arg.Any<CancellationToken>())
            .Returns(BudgetAggregate.Create(BudgetId.New(), HouseholdId, BudgetCurrency.PLN, TestClock.UtcNow));

        await Should.ThrowAsync<ConflictException>(() => Run("USD"));
    }

    /// <summary>Child, Guest and unknown roles may not initialise, and nothing is written.</summary>
    [Theory]
    [InlineData("Child")]
    [InlineData("Guest")]
    [InlineData("Superuser")]
    public async Task Handle_WithNonAdultRole_ThrowsForbidden(string role)
    {
        GivenCaller(role);

        await Should.ThrowAsync<ForbiddenException>(() => Run());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>A caller without a household gets not-found, never an empty budget.</summary>
    [Fact]
    public async Task Handle_WithoutHousehold_ThrowsNotFound()
    {
        _households.GetHouseholdContextForUserAsync("auth|1", Arg.Any<CancellationToken>())
            .Returns((HouseholdContext?)null);

        await Should.ThrowAsync<NotFoundException>(() => Run());
    }

    /// <summary>A failed Household lookup propagates instead of widening or emptying access.</summary>
    [Fact]
    public async Task Handle_WhenHouseholdLookupFails_Propagates()
    {
        _households.GetHouseholdContextForUserAsync("auth|1", Arg.Any<CancellationToken>())
            .Returns<HouseholdContext?>(_ => throw new InvalidOperationException("db down"));

        await Should.ThrowAsync<InvalidOperationException>(() => Run());
    }

    /// <summary>Only PLN, EUR and USD (any case) validate.</summary>
    [Theory]
    [InlineData(null, 0)]
    [InlineData("pln", 0)]
    [InlineData("USD", 0)]
    [InlineData("GBP", 1)]
    [InlineData("1", 1)]
    [InlineData("", 1)]
    public void Validator_AcceptsOnlySupportedCurrencies(string? currency, int errors)
        => new InitializeBudgetCommandValidator()
            .Validate(new InitializeBudgetCommand("auth|1", currency)).Count().ShouldBe(errors);
}
