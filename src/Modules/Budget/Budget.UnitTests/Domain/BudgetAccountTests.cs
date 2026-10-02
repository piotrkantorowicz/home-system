namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;

/// <summary>Invariants of the <c>BudgetAccount</c> (envelope) aggregate.</summary>
public sealed class BudgetAccountTests
{
    /// <summary>A household envelope has no owner and a trimmed name.</summary>
    [Fact]
    public void Create_HouseholdEnvelope_TrimsNameAndHasNoOwner()
    {
        var account = BudgetAccount.Create(
            BudgetAccountId.New(), BudgetId.New(), "  Everyday ", AccountVisibility.Household, null, TestClock.UtcNow);

        account.Name.ShouldBe("Everyday");
        account.OwnerPersonId.ShouldBeNull();
    }

    /// <summary>A personal envelope keeps its owner.</summary>
    [Fact]
    public void Create_PersonalEnvelope_KeepsOwner()
    {
        var owner = Guid.CreateVersion7();

        var account = BudgetAccount.Create(
            BudgetAccountId.New(), BudgetId.New(), "Mine", AccountVisibility.Personal, owner, TestClock.UtcNow);

        account.OwnerPersonId.ShouldBe(owner);
    }

    /// <summary>A personal envelope without an owner, or a household one with an owner, is rejected.</summary>
    [Theory]
    [InlineData(AccountVisibility.Personal, false)]
    [InlineData(AccountVisibility.Household, true)]
    public void Create_OwnerNotMatchingVisibility_Throws(AccountVisibility visibility, bool withOwner)
        => Should.Throw<BudgetDomainException>(() => BudgetAccount.Create(
            BudgetAccountId.New(), BudgetId.New(), "X", visibility, withOwner ? Guid.CreateVersion7() : null, TestClock.UtcNow));

    /// <summary>Blank and over-long names are rejected.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_Throws(string name)
        => Should.Throw<BudgetDomainException>(() => BudgetAccount.Create(
            BudgetAccountId.New(), BudgetId.New(), name, AccountVisibility.Household, null, TestClock.UtcNow));

    /// <summary>A name over the cap is rejected.</summary>
    [Fact]
    public void Create_NameOverLimit_Throws()
        => Should.Throw<BudgetDomainException>(() => BudgetAccount.Create(
            BudgetAccountId.New(), BudgetId.New(), new string('x', BudgetAccount.MaxNameLength + 1),
            AccountVisibility.Household, null, TestClock.UtcNow));
}
