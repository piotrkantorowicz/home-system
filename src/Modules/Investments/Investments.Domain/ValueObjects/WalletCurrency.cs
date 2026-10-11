namespace Investments.Domain.ValueObjects;

/// <summary>The currencies a wallet can hold; all have two decimal places. Immutable once chosen.</summary>
public enum WalletCurrency
{
    /// <summary>Polish złoty — the reporting currency.</summary>
    PLN,

    /// <summary>Euro.</summary>
    EUR,

    /// <summary>US dollar.</summary>
    USD,
}
