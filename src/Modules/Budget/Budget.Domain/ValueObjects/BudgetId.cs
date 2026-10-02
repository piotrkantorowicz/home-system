namespace Budget.Domain.ValueObjects;

/// <summary>Typed identifier of a <c>Budget</c> aggregate; equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record BudgetId(Guid Value)
{
    /// <summary>Generates a new identifier for a budget being created.</summary>
    public static BudgetId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static BudgetId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
