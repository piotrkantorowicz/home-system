namespace Budget.Domain.ValueObjects;

/// <summary>Typed identifier of an <c>Expense</c> aggregate; equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record ExpenseId(Guid Value)
{
    /// <summary>Generates a new identifier for an expense being recorded.</summary>
    public static ExpenseId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static ExpenseId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
