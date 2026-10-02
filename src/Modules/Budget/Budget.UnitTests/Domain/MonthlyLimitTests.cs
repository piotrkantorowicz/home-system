namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Invariants of the <c>MonthlyLimit</c> aggregate and non-negative money parsing.</summary>
public sealed class MonthlyLimitTests
{
    private static readonly BudgetMonth October = ParseMonth("2026-10");

    private static BudgetMonth ParseMonth(string text)
    {
        BudgetMonth.TryParse(text, out var month).ShouldBeTrue();
        return month;
    }

    private static BudgetAccount Account()
        => BudgetAccount.Create(BudgetAccountId.New(), BudgetId.New(), "Everyday", AccountVisibility.Household, null, TestClock.UtcNow);

    /// <summary>A zero limit is a real limit, unlike a missing one; positive-only parsing still rejects zero.</summary>
    [Fact]
    public void Zero_IsAValidLimit_ButNotAValidExpense()
    {
        Money.TryParseNonNegative("0.00", out var zero).ShouldBeTrue();
        Money.TryParsePositive("0.00", out _).ShouldBeFalse();

        var limit = MonthlyLimit.Create(MonthlyLimitId.New(), Account(), October, zero, TestClock.UtcNow);

        limit.Amount.ShouldBe(0m);
        limit.Revision.ShouldBe(1);
    }

    /// <summary>Non-negative parsing keeps the same strictness otherwise.</summary>
    [Theory]
    [InlineData("-1")]
    [InlineData("1.005")]
    [InlineData("1e2")]
    [InlineData("")]
    [InlineData("10000000000.00")]
    public void TryParseNonNegative_Invalid(string text)
        => Money.TryParseNonNegative(text, out _).ShouldBeFalse();

    /// <summary>Changing needs the current revision, bumps it, and a stale one conflicts.</summary>
    [Fact]
    public void Change_BumpsRevision_AndRejectsStale()
    {
        var account = Account();
        var limit = MonthlyLimit.Create(MonthlyLimitId.New(), account, October, Money.FromMinorUnits(10000), TestClock.UtcNow);

        limit.Change(account, Money.FromMinorUnits(20000), 1, TestClock.UtcNow);

        limit.Amount.ShouldBe(200m);
        limit.Revision.ShouldBe(2);
        Should.Throw<ConflictException>(() => limit.Change(account, Money.FromMinorUnits(1), 1, TestClock.UtcNow));
        Should.Throw<ConflictException>(() => limit.EnsureRevision(1));
    }

    /// <summary>An archived envelope cannot get a new limit or have one changed.</summary>
    [Fact]
    public void ArchivedEnvelope_RejectsSetAndChange()
    {
        var account = Account();
        var limit = MonthlyLimit.Create(MonthlyLimitId.New(), account, October, Money.FromMinorUnits(100), TestClock.UtcNow);
        account.Archive(1);

        Should.Throw<BudgetDomainException>(() => MonthlyLimit.Create(MonthlyLimitId.New(), account, October, Money.FromMinorUnits(1), TestClock.UtcNow));
        Should.Throw<BudgetDomainException>(() => limit.Change(account, Money.FromMinorUnits(1), 1, TestClock.UtcNow));
    }
}
