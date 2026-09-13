namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// Typed identifier of a <c>Recipe</c> aggregate. Wrapping the <see cref="Guid"/> stops
/// identifiers of different aggregates being swapped at compile time; equality is by value.
/// </summary>
/// <param name="Value">The underlying database key.</param>
public sealed record RecipeId(Guid Value)
{
    /// <summary>Generates a new, unique identifier for an aggregate being created.</summary>
    public static RecipeId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing key read from storage or a request.</summary>
    /// <param name="value">The raw key.</param>
    public static RecipeId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
