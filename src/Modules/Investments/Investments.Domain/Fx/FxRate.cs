namespace Investments.Domain.Fx;

using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;

/// <summary>
/// One NBP table A mid rate: how many PLN one unit of <see cref="Currency"/> cost on
/// <see cref="EffectiveDate"/>. Reference data, not household-scoped; never edited once stored.
/// </summary>
public sealed class FxRate
{
    /// <summary>Maximum length of an NBP table number such as <c>172/A/NBP/2026</c>.</summary>
    public const int MaxTableNoLength = 32;

    private FxRate() { }

    /// <summary>Creates a rate read from NBP.</summary>
    /// <param name="currency">A foreign currency; PLN has no rate.</param>
    /// <param name="effectiveDate">The business day the rate was published for.</param>
    /// <param name="mid">PLN per unit; positive.</param>
    /// <param name="tableNo">The NBP table number.</param>
    /// <exception cref="InvestmentsDomainException">PLN, a non-positive rate or a blank table number.</exception>
    public static FxRate Create(WalletCurrency currency, DateOnly effectiveDate, decimal mid, string tableNo)
    {
        if (currency == WalletCurrency.PLN)
            throw new InvestmentsDomainException("PLN has no exchange rate.");
        if (mid <= 0)
            throw new InvestmentsDomainException("An exchange rate must be positive.");
        if (string.IsNullOrWhiteSpace(tableNo) || tableNo.Length > MaxTableNoLength)
            throw new InvestmentsDomainException("An NBP table number is required.");

        return new FxRate { Currency = currency, EffectiveDate = effectiveDate, Mid = mid, TableNo = tableNo };
    }

    /// <summary>The foreign currency.</summary>
    public WalletCurrency Currency { get; private set; }

    /// <summary>The business day the rate applies to.</summary>
    public DateOnly EffectiveDate { get; private set; }

    /// <summary>PLN per unit, six decimals.</summary>
    public decimal Mid { get; private set; }

    /// <summary>The NBP table number, e.g. <c>172/A/NBP/2026</c>.</summary>
    public string TableNo { get; private set; } = default!;
}
