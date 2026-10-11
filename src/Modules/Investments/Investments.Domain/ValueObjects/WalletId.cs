namespace Investments.Domain.ValueObjects;

/// <summary>Typed identifier of a <c>Wallet</c> aggregate; equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record WalletId(Guid Value)
{
    /// <summary>Generates a new identifier for a wallet being created.</summary>
    public static WalletId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static WalletId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
