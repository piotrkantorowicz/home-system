namespace DietPlanner.Domain.Entities;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A product actually eaten as part of a <see cref="Aggregates.MealEntry"/> override, with its
/// amount. Created only through <c>MealEntry.ApplyOverride</c>, which validates the line.
/// </summary>
public sealed class MealEntryActualProduct : Entity<MealEntryActualProductId>
{
    private MealEntryActualProduct() { }

    internal static MealEntryActualProduct Create(
        MealEntryActualProductId id,
        ProductId productId,
        decimal amount,
        string unit)
    {
        ArgumentNullException.ThrowIfNull(productId);

        if (amount <= 0)
            throw new DietPlannerDomainException("Actual product amount must be positive.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new DietPlannerDomainException("Actual product unit is required.");

        return new MealEntryActualProduct
        {
            Id = id,
            ProductId = productId,
            Amount = amount,
            Unit = unit
        };
    }

    /// <summary>The product eaten.</summary>
    public ProductId ProductId { get; private set; } = default!;
    /// <summary>Quantity in <see cref="Unit"/>; always positive.</summary>
    public decimal Amount { get; private set; }
    /// <summary>Unit of <see cref="Amount"/>: <c>g</c>, <c>ml</c> or <c>piece</c>.</summary>
    public string Unit { get; private set; } = default!;
}
