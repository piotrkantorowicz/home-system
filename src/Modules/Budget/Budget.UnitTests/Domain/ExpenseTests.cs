namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;

/// <summary>Invariants of the <c>Expense</c> aggregate.</summary>
public sealed class ExpenseTests
{
    private static readonly PersonRef Alex = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Alex");
    private static readonly PersonRef Bea = new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Bea");
    private static readonly PersonRef Cy = new(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Cy");

    private static BudgetAccount Account()
        => BudgetAccount.Create(BudgetAccountId.New(), BudgetId.New(), "Everyday", AccountVisibility.Household, null, TestClock.UtcNow);

    private static Expense Create(
        BudgetAccount? account = null, FundingSource funding = FundingSource.Individual, PersonRef? paidBy = null,
        IReadOnlyCollection<PersonRef>? participants = null, string amount = "100.00")
    {
        return Expense.Create(
            ExpenseId.New(), account ?? Account(), Money.FromDecimal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)), ExpenseCategory.Groceries, new DateOnly(2026, 10, 1), funding,
            funding == FundingSource.Individual ? paidBy ?? Bea : paidBy, Alex, participants ?? [], Guid.CreateVersion7(), TestClock.UtcNow);
    }

    /// <summary>Payer and recorder are separate; shares are stored exactly and revision 1 snapshots them.</summary>
    [Fact]
    public void Create_Shared_StoresSharesAndCreationRevision()
    {
        var expense = Create(participants: [Cy, Alex, Bea]);

        expense.AddedByPersonId.ShouldBe(Alex.PersonId);
        expense.PaidByPersonId.ShouldBe(Bea.PersonId);
        expense.Shares.Select(s => s.Amount).ShouldBe([33.34m, 33.33m, 33.33m]);
        expense.Revision.ShouldBe(1);
        var revision = expense.Revisions.Single();
        revision.RevisionNumber.ShouldBe(1);
        revision.Request.ParticipantIds.ShouldBe([Alex.PersonId, Bea.PersonId, Cy.PersonId]);
        revision.Snapshot.Shares.Select(s => (s.PersonDisplayName, s.Amount)).ShouldBe([("Alex", 33.34m), ("Bea", 33.33m), ("Cy", 33.33m)]);
        revision.Snapshot.AddedByDisplayName.ShouldBe("Alex");
    }

    /// <summary>Household funds credit nobody and are not split.</summary>
    [Fact]
    public void Create_HouseholdFunds_HasNoPayerOrShares()
    {
        var expense = Create(funding: FundingSource.HouseholdFunds);

        expense.PaidByPersonId.ShouldBeNull();
        expense.Shares.ShouldBeEmpty();
    }

    /// <summary>Household funds with a payer or participants are rejected.</summary>
    [Fact]
    public void Create_HouseholdFundsWithPayerOrParticipants_Throws()
    {
        var five = Money.FromMinorUnits(500);
        Should.Throw<BudgetDomainException>(() => Expense.Create(
            ExpenseId.New(), Account(), five, ExpenseCategory.Other, new DateOnly(2026, 10, 1), FundingSource.HouseholdFunds,
            Bea, Alex, [], Guid.CreateVersion7(), TestClock.UtcNow));
        Should.Throw<BudgetDomainException>(() => Expense.Create(
            ExpenseId.New(), Account(), five, ExpenseCategory.Other, new DateOnly(2026, 10, 1), FundingSource.HouseholdFunds,
            null, Alex, [Bea], Guid.CreateVersion7(), TestClock.UtcNow));
    }

    /// <summary>Individual funding needs a payer; duplicate participants are rejected.</summary>
    [Fact]
    public void Create_MissingPayerOrDuplicateParticipant_Throws()
    {
        var five = Money.FromMinorUnits(500);
        Should.Throw<BudgetDomainException>(() => Expense.Create(
            ExpenseId.New(), Account(), five, ExpenseCategory.Other, new DateOnly(2026, 10, 1), FundingSource.Individual,
            null, Alex, [], Guid.CreateVersion7(), TestClock.UtcNow));
        Should.Throw<BudgetDomainException>(() => Create(participants: [Alex, Alex]));
    }

    /// <summary>An archived envelope cannot receive entries.</summary>
    [Fact]
    public void Create_InArchivedEnvelope_Throws()
    {
        var account = Account();
        account.Archive(1);

        Should.Throw<BudgetDomainException>(() => Create(account));
    }

    /// <summary>Request matching ignores participant order only because the request is normalised, and detects any changed field.</summary>
    [Fact]
    public void Request_Matches_DetectsChanges()
    {
        var a = Create(participants: [Alex, Bea]).Revisions.Single().Request;

        a.Matches(a with { }).ShouldBeTrue();
        a.Matches(a with { Amount = "100.01" }).ShouldBeFalse();
        a.Matches(a with { ParticipantIds = [Alex.PersonId] }).ShouldBeFalse();
        a.Matches(a with { Category = ExpenseCategory.Other }).ShouldBeFalse();
    }
}
