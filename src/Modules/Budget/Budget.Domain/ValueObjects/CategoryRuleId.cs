namespace Budget.Domain.ValueObjects;

/// <summary>Typed identifier of a <c>CategoryRule</c> aggregate; equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record CategoryRuleId(Guid Value)
{
    /// <summary>Generates a new identifier for a rule being learned.</summary>
    public static CategoryRuleId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static CategoryRuleId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
