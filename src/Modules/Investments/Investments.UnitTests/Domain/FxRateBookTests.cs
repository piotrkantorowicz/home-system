namespace Investments.UnitTests.Domain;

using Investments.Domain.Exceptions;
using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;

/// <summary>Rate selection: latest published on or before the date, never a later one.</summary>
public sealed class FxRateBookTests
{
    private static readonly FxRate Thursday = FxRate.Create(WalletCurrency.EUR, new DateOnly(2026, 9, 3), 4.25m, "171/A/NBP/2026");
    private static readonly FxRate Friday = FxRate.Create(WalletCurrency.EUR, new DateOnly(2026, 9, 4), 4.2651m, "172/A/NBP/2026");
    private static readonly FxRate UsdFriday = FxRate.Create(WalletCurrency.USD, new DateOnly(2026, 9, 4), 3.6m, "172/A/NBP/2026");

    private static readonly FxRateBook Book = new([Friday, UsdFriday, Thursday]);

    /// <summary>A business day uses its own rate.</summary>
    [Fact]
    public void Find_BusinessDay_UsesThatDay()
        => Book.Find(WalletCurrency.EUR, new DateOnly(2026, 9, 3)).ShouldBe(Thursday);

    /// <summary>A weekend falls back to Friday.</summary>
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void Find_Weekend_UsesPriorBusinessDay(int day)
        => Book.Find(WalletCurrency.EUR, new DateOnly(2026, 9, day)).ShouldBe(Friday);

    /// <summary>A date before every rate has none, rather than borrowing a later one.</summary>
    [Fact]
    public void Find_BeforeFirstRate_IsNull()
        => Book.Find(WalletCurrency.EUR, new DateOnly(2026, 9, 2)).ShouldBeNull();

    /// <summary>Rates of one currency never answer for another.</summary>
    [Fact]
    public void Find_OtherCurrency_IsSeparate()
        => Book.Find(WalletCurrency.USD, new DateOnly(2026, 9, 6)).ShouldBe(UsdFriday);

    /// <summary>Conversion rounds to grosze and carries the rate used.</summary>
    [Fact]
    public void ToPln_Foreign_RoundsAndCarriesRate()
    {
        var value = Book.ToPln(1000.50m, WalletCurrency.EUR, new DateOnly(2026, 9, 6));

        value.Amount.ShouldBe(4267.23m);
        value.Rate.ShouldBe(Friday);
        value.RateMissing.ShouldBeFalse();
    }

    /// <summary>No rate on or before the date means missing, with no amount.</summary>
    [Fact]
    public void ToPln_NoRate_IsMissing()
    {
        var value = Book.ToPln(10m, WalletCurrency.EUR, new DateOnly(2026, 9, 1));

        value.Amount.ShouldBeNull();
        value.RateMissing.ShouldBeTrue();
    }

    /// <summary>PLN needs no rate.</summary>
    [Fact]
    public void ToPln_Pln_IsUnchanged()
        => new FxRateBook([]).ToPln(12.34m, WalletCurrency.PLN, new DateOnly(2026, 9, 1)).ShouldBe(new PlnValue(12.34m, null, false));

    /// <summary>PLN and non-positive rates are rejected.</summary>
    [Fact]
    public void Create_Invalid_Throws()
    {
        Should.Throw<InvestmentsDomainException>(() => FxRate.Create(WalletCurrency.PLN, new DateOnly(2026, 9, 1), 1m, "x"));
        Should.Throw<InvestmentsDomainException>(() => FxRate.Create(WalletCurrency.EUR, new DateOnly(2026, 9, 1), 0m, "x"));
    }
}
