namespace Investments.Domain.ValueObjects;

/// <summary>Who can see a wallet. Separate from ownership; the owner may change it.</summary>
public enum WalletVisibility
{
    /// <summary>Visible to every current Owner/Adult of the household.</summary>
    Shared,

    /// <summary>Visible only to the owner, and to adults when the owner is a managed member.</summary>
    Private,
}
