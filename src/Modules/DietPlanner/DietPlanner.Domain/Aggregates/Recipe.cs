namespace DietPlanner.Domain.Aggregates;

using Shared.Abstractions.Core.Domain;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class Recipe : AggregateRoot<RecipeId>
{
    private readonly List<RecipeIngredient> _ingredients = [];

    private Recipe() { }

    public static Recipe Create(
        RecipeId id,
        string name,
        string? description,
        string? instructions,
        int servings,
        int? prepTimeMinutes,
        string createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);

        return new Recipe
        {
            Id = id,
            Name = name,
            Description = description,
            Instructions = instructions,
            Servings = servings,
            PrepTimeMinutes = prepTimeMinutes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? Instructions { get; private set; }
    public int Servings { get; private set; } = 1;
    public int? PrepTimeMinutes { get; private set; }
    public string CreatedByUserId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    public bool IsDeleted => DeletedAt.HasValue;

    public void Update(
        string name,
        string? description,
        string? instructions,
        int servings,
        int? prepTimeMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
        Instructions = instructions;
        Servings = servings;
        PrepTimeMinutes = prepTimeMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddIngredient(RecipeIngredientId id, ProductId productId, decimal amount, string unit)
    {
        var ingredient = RecipeIngredient.Create(id, productId, amount, unit);
        _ingredients.Add(ingredient);
    }

    public void ClearIngredients()
    {
        _ingredients.Clear();
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            throw new DietPlannerDomainException("Recipe is already deleted.");

        DeletedAt = DateTime.UtcNow;
    }
}
