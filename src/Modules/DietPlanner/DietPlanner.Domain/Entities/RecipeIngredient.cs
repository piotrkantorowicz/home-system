namespace DietPlanner.Domain.Entities;

using Shared.Abstractions.Domain;
using DietPlanner.Domain.ValueObjects;

public sealed class RecipeIngredient : Entity<RecipeIngredientId>
{
    private RecipeIngredient() { }

    internal static RecipeIngredient Create(RecipeIngredientId id, ProductId productId, decimal amount, string unit)
    {
        return new RecipeIngredient
        {
            Id = id,
            ProductId = productId,
            Amount = amount,
            Unit = unit
        };
    }

    public ProductId ProductId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Unit { get; private set; } = "g";
}
