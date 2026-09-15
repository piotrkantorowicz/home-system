namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A food item with its nutrition density per 100 g. Products are the leaves every recipe and
/// meal entry resolve to; they are soft-deleted (see <see cref="SoftDelete"/>) so historical meal
/// entries keep resolving. Created by one user and visible to their household.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    private Product() { }

    /// <summary>Creates a product; the only public construction path.</summary>
    /// <param name="id">Identifier for the new product.</param>
    /// <param name="name">Display name; required.</param>
    /// <param name="nutrition">Nutrition per 100 g; components may be unknown.</param>
    /// <param name="defaultUnit">Unit the UI proposes when this product is added (<c>g</c>, <c>ml</c>, <c>piece</c>).</param>
    /// <param name="densityGramsPerMl">Grams per millilitre for converting volume amounts; 1 g/ml is assumed when <see langword="null"/>.</param>
    /// <param name="gramPerPiece">Grams per piece for converting <c>piece</c> amounts; 100 g is assumed when <see langword="null"/>.</param>
    /// <param name="createdByUserId">Auth subject of the creating user; required.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="createdByUserId"/> is blank.</exception>
    public static Product Create(
        ProductId id,
        string name,
        NutritionPer100g nutrition,
        string defaultUnit,
        decimal? densityGramsPerMl,
        decimal? gramPerPiece,
        string createdByUserId,
        DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);

        return new Product
        {
            Id = id,
            Name = name,
            Nutrition = nutrition,
            DefaultUnit = defaultUnit,
            DensityGramsPerMl = densityGramsPerMl,
            GramPerPiece = gramPerPiece,
            CreatedByUserId = createdByUserId,
            CreatedAt = now
        };
    }

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = default!;
    /// <summary>Nutrition density per 100 g.</summary>
    public NutritionPer100g Nutrition { get; private set; } = default!;
    /// <summary>Unit proposed by default when the product is used; <c>g</c> unless set otherwise.</summary>
    public string DefaultUnit { get; private set; } = "g";
    /// <summary>Grams per millilitre used to convert volume amounts; when <see langword="null"/> <c>UnitConverter</c> assumes water density (1 g/ml).</summary>
    public decimal? DensityGramsPerMl { get; private set; }
    /// <summary>Grams per piece used to convert <c>piece</c> amounts; when <see langword="null"/> <c>UnitConverter</c> assumes 100 g per piece.</summary>
    public decimal? GramPerPiece { get; private set; }
    /// <summary>Auth subject of the user who created the product.</summary>
    public string CreatedByUserId { get; private set; } = default!;
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last <see cref="Update"/> or <see cref="Restore"/>, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }
    /// <summary>When the product was soft-deleted, UTC; <see langword="null"/> while active.</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>Whether the product is currently soft-deleted.</summary>
    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>Replaces every editable field at once and stamps <see cref="UpdatedAt"/>.</summary>
    /// <param name="name">New display name; required.</param>
    /// <param name="nutrition">New nutrition per 100 g.</param>
    /// <param name="defaultUnit">New default unit.</param>
    /// <param name="densityGramsPerMl">New grams-per-millilitre, or <see langword="null"/> to clear it.</param>
    /// <param name="gramPerPiece">New grams-per-piece, or <see langword="null"/> to clear it.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public void Update(
        string name,
        NutritionPer100g nutrition,
        string defaultUnit,
        decimal? densityGramsPerMl,
        decimal? gramPerPiece,
        DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Nutrition = nutrition;
        DefaultUnit = defaultUnit;
        DensityGramsPerMl = densityGramsPerMl;
        GramPerPiece = gramPerPiece;
        UpdatedAt = now;
    }

    /// <summary>Hides the product from lists and searches without breaking meals that reference it.</summary>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="DietPlannerDomainException">The product is already deleted.</exception>
    public void SoftDelete(DateTime now)
    {
        if (IsDeleted)
            throw new DietPlannerDomainException("Product is already deleted.");

        DeletedAt = now;
    }

    /// <summary>Reverses <see cref="SoftDelete"/>.</summary>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="DietPlannerDomainException">The product is not deleted.</exception>
    public void Restore(DateTime now)
    {
        if (!IsDeleted)
            throw new DietPlannerDomainException("Product is not deleted.");

        DeletedAt = null;
        UpdatedAt = now;
    }
}
