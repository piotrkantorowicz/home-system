namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// Typed identifier of a <c>MealEntryActualProduct</c> entity inside a meal entry. Wrapping the <see cref="Guid"/> stops
/// identifiers of different aggregates being swapped at compile time; equality is by value.
/// </summary>
/// <param name="Value">The underlying database key.</param>
public sealed record MealEntryActualProductId(Guid Value)
{
    /// <summary>Generates a new, unique identifier for an aggregate being created.</summary>
    public static MealEntryActualProductId New() => new(Guid.CreateVersion7());

    /// <summary>Wraps an existing key read from storage or a request.</summary>
    /// <param name="value">The raw key.</param>
    public static MealEntryActualProductId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
