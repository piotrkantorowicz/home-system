namespace DietPlanner.Domain.Entities;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One line of a <see cref="Aggregates.Recipe"/>: a product and how much of it the whole recipe
/// (all <c>Servings</c>) uses. Created only through <c>Recipe.AddIngredient</c>; amounts are
/// converted to grams by <c>UnitConverter</c> when nutrition is calculated.
/// </summary>
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

    /// <summary>The product this line refers to.</summary>
    public ProductId ProductId { get; private set; } = default!;
    /// <summary>Quantity in <see cref="Unit"/> for the full recipe.</summary>
    public decimal Amount { get; private set; }
    /// <summary>Unit of <see cref="Amount"/>: <c>g</c>, <c>ml</c> or <c>piece</c>.</summary>
    public string Unit { get; private set; } = "g";
}
