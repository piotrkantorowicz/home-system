namespace Investments.Domain.ValueObjects;

/// <summary>Typed identifier of a <c>BalanceSnapshot</c> aggregate; equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record BalanceSnapshotId(Guid Value)
{
    /// <summary>Generates a new identifier for a snapshot being recorded.</summary>
    public static BalanceSnapshotId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static BalanceSnapshotId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
