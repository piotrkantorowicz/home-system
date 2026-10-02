namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

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

    private static BudgetAccount NewAccount()
        => BudgetAccount.Create(BudgetAccountId.New(), BudgetId.New(), "Everyday", AccountVisibility.Household, null, TestClock.UtcNow);

    /// <summary>A new envelope starts active at revision 1.</summary>
    [Fact]
    public void Create_StartsActiveAtRevisionOne()
    {
        var account = NewAccount();

        account.IsArchived.ShouldBeFalse();
        account.Revision.ShouldBe(1);
    }

    /// <summary>Rename trims, bumps the revision, and leaves visibility/owner alone.</summary>
    [Fact]
    public void Rename_ChangesNameAndBumpsRevision()
    {
        var account = NewAccount();

        account.Rename("  Holidays ", 1);

        account.Name.ShouldBe("Holidays");
        account.Revision.ShouldBe(2);
        account.Visibility.ShouldBe(AccountVisibility.Household);
    }

    /// <summary>Renaming to the same name changes nothing.</summary>
    [Fact]
    public void Rename_SameName_IsNoOp()
    {
        var account = NewAccount();

        account.Rename("Everyday", 1);

        account.Revision.ShouldBe(1);
    }

    /// <summary>A blank new name is rejected and the envelope keeps its name and revision.</summary>
    [Fact]
    public void Rename_BlankName_Throws()
    {
        var account = NewAccount();

        Should.Throw<BudgetDomainException>(() => account.Rename(" ", 1));

        account.Name.ShouldBe("Everyday");
        account.Revision.ShouldBe(1);
    }

    /// <summary>Every mutation rejects a stale expected revision.</summary>
    [Fact]
    public void Mutations_WithStaleRevision_Conflict()
    {
        var account = NewAccount();
        account.Rename("A", 1);

        Should.Throw<ConflictException>(() => account.Rename("B", 1));
        Should.Throw<ConflictException>(() => account.Archive(1));
        Should.Throw<ConflictException>(() => account.Restore(1));
    }

    /// <summary>Archive then restore round-trips and bumps the revision each time; repeats are no-ops.</summary>
    [Fact]
    public void ArchiveAndRestore_RoundTrip()
    {
        var account = NewAccount();

        account.Archive(1);
        account.Archive(2);
        account.IsArchived.ShouldBeTrue();
        account.Revision.ShouldBe(2);

        account.Restore(2);
        account.Restore(3);
        account.IsArchived.ShouldBeFalse();
        account.Revision.ShouldBe(3);
    }
}

