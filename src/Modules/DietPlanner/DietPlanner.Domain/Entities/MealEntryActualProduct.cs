namespace DietPlanner.Domain.Entities;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

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

    public ProductId ProductId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Unit { get; private set; } = default!;
}
