namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;

/// <summary>#487 — the optional expense description: normalisation, length limit, snapshots.</summary>
public sealed class ExpenseDescriptionTests
{
    private static readonly PersonRef Alex = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Alex");

    private static Expense Create(string? description)
    {
        var account = BudgetAccount.Create(BudgetAccountId.New(), BudgetId.New(), "Everyday", AccountVisibility.Household, null, TestClock.UtcNow);
        return Expense.Create(
            ExpenseId.New(), account, Money.FromMinorUnits(1000), ExpenseCategory.Groceries, new DateOnly(2026, 10, 1),
            FundingSource.HouseholdFunds, null, Alex, [], Guid.CreateVersion7(), TestClock.UtcNow, description);
    }

    private static void Update(Expense e, string? description)
        => e.Update(
            e.Revision, "fix", Money.FromMinorUnits(1000), ExpenseCategory.Groceries, new DateOnly(2026, 10, 1), FundingSource.HouseholdFunds,
            null, [], Alex, Guid.CreateVersion7(),
            new ExpenseRequest("Update", e.BudgetAccountId.Value, "10.00", ExpenseCategory.Groceries, new DateOnly(2026, 10, 1), FundingSource.HouseholdFunds, null, [], e.Id.Value, e.Revision, "fix", description),
            TestClock.UtcNow, description);

    /// <summary>Blank means no description; whitespace runs collapse; 80 characters is the limit.</summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   \t ", null)]
    [InlineData("  Weekly   shop \n at Lidl ", "Weekly shop at Lidl")]
    public void TryNormalizeDescription_NormalizesWhitespace(string? input, string? expected)
    {
        Expense.TryNormalizeDescription(input, out var normalized).ShouldBeTrue();
        normalized.ShouldBe(expected);
    }

    /// <summary>The limit applies after normalising: 80 passes, 81 fails, padding does not count.</summary>
    [Fact]
    public void Description_Length_LimitIs80AfterNormalising()
    {
        Expense.TryNormalizeDescription(new string('a', 80), out _).ShouldBeTrue();
        Expense.TryNormalizeDescription($"  {new string('a', 80)}  ", out _).ShouldBeTrue();
        Expense.TryNormalizeDescription(new string('a', 81), out var tooLong).ShouldBeFalse();
        tooLong.ShouldBeNull();
        Should.Throw<BudgetDomainException>(() => Create(new string('a', 81)));
    }

    /// <summary>Create stores the description and revision 1 snapshots it.</summary>
    [Fact]
    public void Create_StoresDescriptionInSnapshot()
    {
        var expense = Create(" Weekly  shop ");

        expense.Description.ShouldBe("Weekly shop");
        expense.Revisions.Single().Snapshot.Description.ShouldBe("Weekly shop");
    }

    /// <summary>Correcting replaces or clears the description; earlier snapshots keep theirs.</summary>
    [Fact]
    public void Update_ReplacesAndClearsDescription_HistoryKeepsPrevious()
    {
        var expense = Create("first");

        Update(expense, "second");
        Update(expense, null);

        expense.Description.ShouldBeNull();
        expense.Revisions.Select(r => r.Snapshot.Description).ShouldBe(["first", "second", null]);
    }

    /// <summary>Replay detection treats a different description as a different request.</summary>
    [Fact]
    public void Request_Matches_ComparesDescription()
    {
        var a = Create("a").Revisions.Single().Request;

        a.Matches(a with { Description = "a" }).ShouldBeTrue();
        a.Matches(a with { Description = "b" }).ShouldBeFalse();
        a.Matches(a with { Description = null }).ShouldBeFalse();
    }
}
