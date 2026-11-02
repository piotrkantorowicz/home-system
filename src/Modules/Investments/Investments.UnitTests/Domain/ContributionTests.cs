namespace Investments.UnitTests.Domain;

using Investments.Domain.Aggregates;
using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;

/// <summary>Invariants of the <c>Contribution</c> aggregate.</summary>
public sealed class ContributionTests
{
    private static readonly DateOnly Today = new(2026, 10, 2);

    private static Wallet NewWallet() => Wallet.Create(
        WalletId.New(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Anna", "XTB",
        WalletInstitution.XTB, WalletKind.Brokerage, WalletCurrency.PLN, WalletVisibility.Shared, TestClock.Now);

    private static Money Amount(string text) => Money.TryParse(text, out var money) ? money : throw new ArgumentException(text);

    private static Contribution Record(
        Wallet wallet, DateOnly date, string amount = "100", string? note = null,
        ContributionDirection direction = ContributionDirection.Deposit)
        => Contribution.Record(
            ContributionId.New(), wallet, date, direction, Amount(amount), note, Guid.CreateVersion7(), "Anna", Today, TestClock.Now);

    /// <summary>A recorded contribution copies the wallet's household and trims the note.</summary>
    [Fact]
    public void Record_Valid_CopiesWalletAndTrimsNote()
    {
        var wallet = NewWallet();

        var contribution = Record(wallet, Today, "500.5", "  monthly top-up ", ContributionDirection.Withdrawal);

        contribution.WalletId.ShouldBe(wallet.Id);
        contribution.HouseholdId.ShouldBe(wallet.HouseholdId);
        contribution.Direction.ShouldBe(ContributionDirection.Withdrawal);
        contribution.Amount.ShouldBe(500.50m);
        contribution.Note.ShouldBe("monthly top-up");
    }

    /// <summary>A zero amount is rejected.</summary>
    [Fact]
    public void Record_Zero_Throws()
        => Should.Throw<InvestmentsDomainException>(() => Record(NewWallet(), Today, "0"));

    /// <summary>A date after today is rejected.</summary>
    [Fact]
    public void Record_FutureDate_Throws()
        => Should.Throw<InvestmentsDomainException>(() => Record(NewWallet(), Today.AddDays(1)));

    /// <summary>A date before NBP's first table is rejected; the first table date itself is fine.</summary>
    [Fact]
    public void Record_BeforeEarliestDate_Throws()
    {
        Should.Throw<InvestmentsDomainException>(() => Record(NewWallet(), BalanceSnapshot.EarliestAsOf.AddDays(-1)));
        Record(NewWallet(), BalanceSnapshot.EarliestAsOf).Date.ShouldBe(BalanceSnapshot.EarliestAsOf);
    }

    /// <summary>An archived wallet takes no new contributions.</summary>
    [Fact]
    public void Record_ArchivedWallet_Throws()
    {
        var wallet = NewWallet();
        wallet.Archive(Today);

        Should.Throw<InvestmentsDomainException>(() => Record(wallet, Today));
    }

    /// <summary>A note over the cap is rejected.</summary>
    [Fact]
    public void Record_NoteTooLong_Throws()
        => Should.Throw<InvestmentsDomainException>(() => Record(NewWallet(), Today, note: new string('x', Contribution.MaxNoteLength + 1)));
}
