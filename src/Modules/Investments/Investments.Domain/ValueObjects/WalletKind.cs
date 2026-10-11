namespace Investments.Domain.ValueObjects;

/// <summary>What kind of account a wallet is. Drives grouping now and tax treatment later. Immutable.</summary>
public enum WalletKind
{
    /// <summary>Ordinary brokerage account.</summary>
    Brokerage,

    /// <summary>Individual retirement account (IKE).</summary>
    IKE,

    /// <summary>Individual retirement security account (IKZE).</summary>
    IKZE,

    /// <summary>Employee capital plan (PPK).</summary>
    PPK,

    /// <summary>Polish treasury bonds.</summary>
    TreasuryBonds,

    /// <summary>Savings account or deposit.</summary>
    Savings,

    /// <summary>Cash.</summary>
    Cash,
}
