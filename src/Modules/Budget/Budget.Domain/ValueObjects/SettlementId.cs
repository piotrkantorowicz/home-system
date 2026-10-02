namespace Budget.Domain.ValueObjects;

/// <summary>Typed identifier of a <c>Settlement</c> (recorded repayment); equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record SettlementId(Guid Value)
{
    /// <summary>Generates a new identifier for a repayment being recorded.</summary>
    public static SettlementId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static SettlementId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
