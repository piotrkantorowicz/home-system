namespace Investments.Domain.ValueObjects;

/// <summary>Where a wallet is held. Display and grouping only.</summary>
public enum WalletInstitution
{
    /// <summary>XTB.</summary>
    XTB,

    /// <summary>Saxo Bank.</summary>
    Saxo,

    /// <summary>Bossa (DM BOŚ).</summary>
    Bossa,

    /// <summary>mBank.</summary>
    MBank,

    /// <summary>PKO BP.</summary>
    PKOBP,

    /// <summary>Allianz.</summary>
    Allianz,

    /// <summary>Any other institution.</summary>
    Other,
}
