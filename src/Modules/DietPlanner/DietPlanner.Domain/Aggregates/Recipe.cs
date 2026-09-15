namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A named list of <see cref="RecipeIngredient"/> lines that yields <see cref="Servings"/> portions.
/// Nutrition is not stored — it is calculated from the ingredients' products on read. Recipes are
/// soft-deleted so past meal entries keep their reference.
/// </summary>
public sealed class Recipe : AggregateRoot<RecipeId>
{
    private readonly List<RecipeIngredient> _ingredients = [];

    private Recipe() { }

    /// <summary>Creates a recipe with no ingredients; add them with <see cref="AddIngredient"/>.</summary>
    /// <param name="id">Identifier for the new recipe.</param>
    /// <param name="name">Display name; required.</param>
    /// <param name="description">Optional free-text description.</param>
    /// <param name="instructions">Optional preparation steps.</param>
    /// <param name="servings">How many portions the ingredient amounts yield.</param>
    /// <param name="prepTimeMinutes">Optional preparation time.</param>
    /// <param name="createdByUserId">Auth subject of the creating user; required.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="createdByUserId"/> is blank.</exception>
    public static Recipe Create(
        RecipeId id,
        string name,
        string? description,
        string? instructions,
        int servings,
        int? prepTimeMinutes,
        string createdByUserId,
        DateTime now)
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
            CreatedAt = now
        };
    }

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = default!;
    /// <summary>Optional free-text description.</summary>
    public string? Description { get; private set; }
    /// <summary>Optional preparation steps.</summary>
    public string? Instructions { get; private set; }
    /// <summary>Portions the ingredient amounts yield; per-serving nutrition divides by this.</summary>
    public int Servings { get; private set; } = 1;
    /// <summary>Optional preparation time in minutes.</summary>
    public int? PrepTimeMinutes { get; private set; }
    /// <summary>Auth subject of the user who created the recipe.</summary>
    public string CreatedByUserId { get; private set; } = default!;
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last <see cref="Update"/>, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }
    /// <summary>When the recipe was soft-deleted, UTC; <see langword="null"/> while active.</summary>
    public DateTime? DeletedAt { get; private set; }
    /// <summary>The ingredient lines, in insertion order.</summary>
    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    /// <summary>Whether the recipe is currently soft-deleted.</summary>
    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>Replaces the header fields (not the ingredients) and stamps <see cref="UpdatedAt"/>.</summary>
    /// <param name="name">New display name; required.</param>
    /// <param name="description">New description, or <see langword="null"/> to clear it.</param>
    /// <param name="instructions">New instructions, or <see langword="null"/> to clear them.</param>
    /// <param name="servings">New number of portions.</param>
    /// <param name="prepTimeMinutes">New preparation time, or <see langword="null"/> to clear it.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public void Update(
        string name,
        string? description,
        string? instructions,
        int servings,
        int? prepTimeMinutes,
        DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
        Instructions = instructions;
        Servings = servings;
        PrepTimeMinutes = prepTimeMinutes;
        UpdatedAt = now;
    }

    /// <summary>Appends an ingredient line. Callers replacing the whole list call <see cref="ClearIngredients"/> first.</summary>
    /// <param name="id">Identifier for the new line.</param>
    /// <param name="productId">The product used.</param>
    /// <param name="amount">Quantity for the full recipe.</param>
    /// <param name="unit">Unit of <paramref name="amount"/>: <c>g</c>, <c>ml</c> or <c>piece</c>.</param>
    public void AddIngredient(RecipeIngredientId id, ProductId productId, decimal amount, string unit)
    {
        var ingredient = RecipeIngredient.Create(id, productId, amount, unit);
        _ingredients.Add(ingredient);
    }

    /// <summary>Removes every ingredient line; used when an update replaces the list wholesale.</summary>
    public void ClearIngredients()
    {
        _ingredients.Clear();
    }

    /// <summary>Hides the recipe from lists and searches without breaking meals that reference it.</summary>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="DietPlannerDomainException">The recipe is already deleted.</exception>
    public void SoftDelete(DateTime now)
    {
        if (IsDeleted)
            throw new DietPlannerDomainException("Recipe is already deleted.");

        DeletedAt = now;
    }
}
