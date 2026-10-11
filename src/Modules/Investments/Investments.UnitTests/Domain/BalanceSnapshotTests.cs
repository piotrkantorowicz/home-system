namespace Investments.UnitTests.Domain;

using Investments.Domain.Aggregates;
using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;

/// <summary>Invariants of the <c>BalanceSnapshot</c> aggregate.</summary>
public sealed class BalanceSnapshotTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);

    private static Wallet NewWallet() => Wallet.Create(
        WalletId.New(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Anna", "XTB",
        WalletInstitution.XTB, WalletKind.Brokerage, WalletCurrency.PLN, WalletVisibility.Shared, TestClock.Now);

    private static Money Amount(string text) => Money.TryParse(text, out var money) ? money : throw new ArgumentException(text);

    private static BalanceSnapshot Record(Wallet wallet, DateOnly asOf, string amount = "100", string? note = null)
        => BalanceSnapshot.Record(
            BalanceSnapshotId.New(), wallet, asOf, Amount(amount), note, Guid.CreateVersion7(), "Anna", Today, TestClock.Now);

    /// <summary>A recorded balance copies the wallet's household and trims the note.</summary>
    [Fact]
    public void Record_Valid_CopiesWalletAndTrimsNote()
    {
        var wallet = NewWallet();

        var snapshot = Record(wallet, Today, "12450.10", "  after bonus ");

        snapshot.WalletId.ShouldBe(wallet.Id);
        snapshot.HouseholdId.ShouldBe(wallet.HouseholdId);
        snapshot.Amount.ShouldBe(12450.10m);
        snapshot.Note.ShouldBe("after bonus");
    }

    /// <summary>A date after today is rejected.</summary>
    [Fact]
    public void Record_FutureDate_Throws()
        => Should.Throw<InvestmentsDomainException>(() => Record(NewWallet(), Today.AddDays(1)));

    /// <summary>A date before NBP's first table is rejected; the first table date itself is fine.</summary>
    [Fact]
    public void Record_BeforeEarliestDate_Throws()
    {
        Should.Throw<InvestmentsDomainException>(() => Record(NewWallet(), BalanceSnapshot.EarliestAsOf.AddDays(-1)));
        Record(NewWallet(), BalanceSnapshot.EarliestAsOf).AsOf.ShouldBe(BalanceSnapshot.EarliestAsOf);
    }

    /// <summary>An archived wallet takes no new dates.</summary>
    [Fact]
    public void Record_ArchivedWallet_Throws()
    {
        var wallet = NewWallet();
        wallet.Archive();

        Should.Throw<InvestmentsDomainException>(() => Record(wallet, Today));
    }

    /// <summary>A note over the cap is rejected.</summary>
    [Fact]
    public void Record_NoteTooLong_Throws()
        => Should.Throw<InvestmentsDomainException>(() => Record(NewWallet(), Today, note: new string('x', BalanceSnapshot.MaxNoteLength + 1)));

    /// <summary>A correction replaces amount and note and moves the update time; a no-op keeps it.</summary>
    [Fact]
    public void Correct_ReplacesAndStampsOnlyOnChange()
    {
        var snapshot = Record(NewWallet(), Today, "100", "x");
        var later = TestClock.Now.AddHours(1);

        snapshot.Correct(Amount("100"), "x", later);
        snapshot.UpdatedAt.ShouldBe(TestClock.Now);

        snapshot.Correct(Amount("150"), " ", later);
        snapshot.Amount.ShouldBe(150m);
        snapshot.Note.ShouldBeNull();
        snapshot.UpdatedAt.ShouldBe(later);
    }
}
