namespace Budget.Domain.ValueObjects;

/// <summary>Typed identifier of a <c>BudgetAccount</c> aggregate; equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record BudgetAccountId(Guid Value)
{
    /// <summary>Generates a new identifier for an envelope being created.</summary>
    public static BudgetAccountId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static BudgetAccountId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
