namespace Investments.UnitTests.Domain;

using Investments.Domain.Fx;
using Investments.Domain.NetWorth;
using Investments.Domain.ValueObjects;

/// <summary>Net worth on a date and the month-end series.</summary>
public sealed class NetWorthCalculatorTests
{
    private static readonly NetWorthWallet Pln = new(WalletId.New(), WalletKind.IKE, WalletCurrency.PLN, Guid.CreateVersion7());
    private static readonly NetWorthWallet Eur = new(WalletId.New(), WalletKind.Brokerage, WalletCurrency.EUR, Guid.CreateVersion7());

    private static readonly NetWorthBalance[] Balances =
    [
        new(Pln.Id, new DateOnly(2026, 1, 31), 1000m),
        new(Pln.Id, new DateOnly(2026, 3, 15), 1500m),
        new(Eur.Id, new DateOnly(2026, 2, 10), 100m),
    ];

    private static NetWorthCalculator Calculator(params FxQuote[] rates) => new([Pln, Eur], Balances, new FxRateBook(rates));

    /// <summary>Each wallet's latest balance on or before the date counts; later ones do not.</summary>
    [Fact]
    public void ValueAt_UsesLatestBalanceOnOrBeforeDate()
    {
        var calculator = Calculator(new FxQuote(WalletCurrency.EUR, new DateOnly(2026, 2, 27), 4.3m, "040/A/NBP/2026"));

        var point = calculator.ValueAt(new DateOnly(2026, 2, 28));

        point.Total.ShouldBe(1000m + 430m);
        point.Incomplete.ShouldBeFalse();
    }

    /// <summary>A foreign balance is converted at the reference date's rate, falling back to the prior business day.</summary>
    [Fact]
    public void ValueAt_ForeignUsesRateForReferenceDate()
    {
        var calculator = Calculator(
            new FxQuote(WalletCurrency.EUR, new DateOnly(2026, 2, 10), 4.0m, "028/A/NBP/2026"),
            new FxQuote(WalletCurrency.EUR, new DateOnly(2026, 3, 13), 4.5m, "050/A/NBP/2026"));

        calculator.ValueAt(new DateOnly(2026, 3, 15)).Total.ShouldBe(1500m + 450m);
    }

    /// <summary>The current figure values a past date's balances at the newest rate, not that date's.</summary>
    [Fact]
    public void ValueAtLatestRates_UsesNewestRate()
    {
        var calculator = Calculator(
            new FxQuote(WalletCurrency.EUR, new DateOnly(2026, 2, 10), 4.0m, "028/A/NBP/2026"),
            new FxQuote(WalletCurrency.EUR, new DateOnly(2026, 3, 13), 4.5m, "050/A/NBP/2026"));

        calculator.ValueAtLatestRates(new DateOnly(2026, 2, 28)).Total.ShouldBe(1000m + 450m);
    }

    /// <summary>A foreign balance without a rate is left out and marks the point incomplete.</summary>
    [Fact]
    public void ValueAt_MissingRate_ExcludedAndIncomplete()
    {
        var point = Calculator().ValueAt(new DateOnly(2026, 2, 28));

        point.Total.ShouldBe(1000m);
        point.Incomplete.ShouldBeTrue();
        point.Wallets.Single(v => v.Wallet == Eur).Pln!.RateMissing.ShouldBeTrue();
    }

    /// <summary>Before any balance a wallet contributes nothing and is not incomplete.</summary>
    [Fact]
    public void ValueAt_BeforeAnyBalance_IsZero()
    {
        var point = Calculator().ValueAt(new DateOnly(2026, 1, 1));

        point.Total.ShouldBe(0m);
        point.Incomplete.ShouldBeFalse();
    }

    /// <summary>Net contributions on or before the date are invested; balance − invested is the gain.</summary>
    [Fact]
    public void ValueAt_WithContributions_InvestedAndGain()
    {
        var calculator = new NetWorthCalculator(
            [Pln], Balances, new FxRateBook([]),
            [
                new(Pln.Id, new DateOnly(2026, 1, 5), 1000m),
                new(Pln.Id, new DateOnly(2026, 2, 5), 500m),
                new(Pln.Id, new DateOnly(2026, 3, 1), -200m),
                new(Pln.Id, new DateOnly(2026, 4, 1), 9999m),
            ]);

        var point = calculator.ValueAt(new DateOnly(2026, 3, 31));

        var wallet = point.Wallets.ShouldHaveSingleItem();
        wallet.Invested.ShouldBe(1300m);
        wallet.GainPln.ShouldBe(200m);
        point.Invested.ShouldBe(1300m);
        point.Gain.ShouldBe(200m);
    }

    /// <summary>A wallet without contributions has no invested figure and stays out of the totals.</summary>
    [Fact]
    public void ValueAt_WithoutContributions_InvestedNull()
    {
        var point = Calculator().ValueAt(new DateOnly(2026, 3, 31));

        point.Wallets.ShouldAllBe(v => v.Invested == null && v.GainPln == null);
        point.Invested.ShouldBeNull();
        point.Gain.ShouldBeNull();
    }

    /// <summary>Foreign net contributions are converted at the balance's rate, so gain excludes currency moves.</summary>
    [Fact]
    public void ValueAt_ForeignContributions_ConvertedAtBalanceRate()
    {
        var calculator = new NetWorthCalculator(
            [Eur], Balances, new FxRateBook([new FxQuote(WalletCurrency.EUR, new DateOnly(2026, 2, 27), 4.3m, "040/A/NBP/2026")]),
            [new(Eur.Id, new DateOnly(2026, 2, 1), 80m)]);

        var wallet = calculator.ValueAt(new DateOnly(2026, 2, 28)).Wallets.ShouldHaveSingleItem();

        wallet.InvestedPln!.Amount.ShouldBe(344m);
        wallet.GainPln.ShouldBe(86m);
    }

    /// <summary>A foreign wallet without a rate is left out of invested and gain; incomplete is unchanged.</summary>
    [Fact]
    public void ValueAt_ForeignContributionsMissingRate_Excluded()
    {
        var calculator = new NetWorthCalculator(
            [Pln, Eur], Balances, new FxRateBook([]),
            [new(Pln.Id, new DateOnly(2026, 1, 5), 1000m), new(Eur.Id, new DateOnly(2026, 2, 1), 80m)]);

        var point = calculator.ValueAt(new DateOnly(2026, 2, 28));

        point.Wallets.Single(v => v.Wallet == Eur).InvestedPln!.Amount.ShouldBeNull();
        point.Invested.ShouldBe(1000m);
        point.Gain.ShouldBe(0m);
        point.Incomplete.ShouldBeTrue();
    }

    /// <summary>Month ends inside the range, plus the last date when it is mid-month.</summary>
    [Fact]
    public void MonthEnds_IncludesCurrentMonthSoFar()
        => NetWorthCalculator.MonthEnds(new DateOnly(2025, 12, 15), new DateOnly(2026, 3, 10)).ShouldBe(
            [new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 10)]);

    /// <summary>A range ending on a month end does not repeat it.</summary>
    [Fact]
    public void MonthEnds_EndingOnMonthEnd_NoDuplicate()
        => NetWorthCalculator.MonthEnds(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28)).ShouldBe(
            [new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28)]);
}
