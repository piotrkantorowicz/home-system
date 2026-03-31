namespace DietPlanner.Domain.Aggregates;

using Shared.Abstractions.Domain;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class Product : AggregateRoot<ProductId>
{
    private Product() { }

    public static Product Create(
        ProductId id,
        string name,
        NutritionPer100g nutrition,
        string defaultUnit,
        decimal? densityGramsPerMl,
        decimal? gramPerPiece,
        string createdByUserId)
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
            CreatedAt = DateTime.UtcNow
        };
    }

    public string Name { get; private set; } = default!;
    public NutritionPer100g Nutrition { get; private set; } = default!;
    public string DefaultUnit { get; private set; } = "g";
    public decimal? DensityGramsPerMl { get; private set; }
    public decimal? GramPerPiece { get; private set; }
    public string CreatedByUserId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void Update(
        string name,
        NutritionPer100g nutrition,
        string defaultUnit,
        decimal? densityGramsPerMl,
        decimal? gramPerPiece)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Nutrition = nutrition;
        DefaultUnit = defaultUnit;
        DensityGramsPerMl = densityGramsPerMl;
        GramPerPiece = gramPerPiece;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            throw new DietPlannerDomainException("Product is already deleted.");

        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
            throw new DietPlannerDomainException("Product is not deleted.");

        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
