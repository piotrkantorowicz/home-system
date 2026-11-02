namespace Investments.Domain.ValueObjects;

/// <summary>Typed identifier of a <c>Contribution</c> aggregate; equality is by value.</summary>
/// <param name="Value">The underlying database key.</param>
public sealed record ContributionId(Guid Value)
{
    /// <summary>Generates a new identifier for a contribution being recorded.</summary>
    public static ContributionId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage.</summary>
    /// <param name="value">The raw key.</param>
    public static ContributionId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
