namespace DietPlanner.Domain.Ledgers;

using DietPlanner.Domain.ValueObjects;

/// <summary>
/// A shopping-list row marked as bought. NOT a DDD aggregate root — a flat row keyed on the shared
/// scope (household, or the caller alone without one), the normalized date range, product and
/// unit, so the same product in two units and the same list over two ranges check independently.
/// </summary>
public sealed class ShoppingCheck
{
    private ShoppingCheck() { }

    /// <summary>Marks one row bought.</summary>
    /// <param name="scopeId">Household identifier, or the caller's person identifier without a household.</param>
    /// <param name="from">First day of the range, or <see langword="null"/> for no lower bound.</param>
    /// <param name="to">Last day of the range, or <see langword="null"/> for no upper bound.</param>
    /// <param name="productId">The product.</param>
    /// <param name="unit">The unit the row is summed in.</param>
    /// <param name="checkedByPersonId">Who ticked it.</param>
    /// <param name="checkedAtUtc">When, UTC.</param>
    /// <exception cref="ArgumentNullException"><paramref name="productId"/> is null.</exception>
    public static ShoppingCheck Create(
        Guid scopeId, DateOnly? from, DateOnly? to, ProductId productId, string unit, Guid checkedByPersonId, DateTime checkedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(productId);
        return new ShoppingCheck
        {
            ScopeId = scopeId,
            RangeFrom = Bound(from, DateOnly.MinValue),
            RangeTo = Bound(to, DateOnly.MaxValue),
            ProductId = productId,
            Unit = unit,
            CheckedByPersonId = checkedByPersonId,
            CheckedAt = checkedAtUtc
        };
    }

    /// <summary>Stored form of an open range bound, so every key column is non-null.</summary>
    /// <param name="bound">The requested bound.</param>
    /// <param name="open">The value standing for "no bound".</param>
    public static DateOnly Bound(DateOnly? bound, DateOnly open) => bound ?? open;

    /// <summary>Household identifier, or the person identifier without a household.</summary>
    public Guid ScopeId { get; private init; }
    /// <summary>First day of the range; <see cref="DateOnly.MinValue"/> when open.</summary>
    public DateOnly RangeFrom { get; private init; }
    /// <summary>Last day of the range; <see cref="DateOnly.MaxValue"/> when open.</summary>
    public DateOnly RangeTo { get; private init; }
    /// <summary>The product.</summary>
    public ProductId ProductId { get; private init; } = default!;
    /// <summary>The unit the row is summed in.</summary>
    public string Unit { get; private init; } = default!;
    /// <summary>Who ticked it.</summary>
    public Guid CheckedByPersonId { get; private init; }
    /// <summary>When it was ticked, UTC.</summary>
    public DateTime CheckedAt { get; private init; }
}
