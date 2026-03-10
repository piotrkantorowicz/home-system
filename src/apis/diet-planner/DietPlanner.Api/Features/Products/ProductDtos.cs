using System.ComponentModel;
using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Features.Products;

/// <summary>
/// Shared interface used by validators to avoid rule duplication between Create and Update.
/// </summary>
public interface IProductRequest
{
    string Name { get; }
    decimal? CaloriesPer100g { get; }
    decimal? ProteinPer100g { get; }
    decimal? CarbsPer100g { get; }
    decimal? FatPer100g { get; }
    decimal? FiberPer100g { get; }
    string DefaultUnit { get; }
    decimal? DensityGramsPerMl { get; }
    decimal? GramPerPiece { get; }
}

/// <summary>
/// Request body for creating a new product.
/// </summary>
public record CreateProductRequest(
    [property: Description("Product name (must be unique, max 200 chars)")] string Name,
    [property: Description("Calories per 100g (0-9000)")] decimal? CaloriesPer100g,
    [property: Description("Protein per 100g in grams (0-100)")] decimal? ProteinPer100g,
    [property: Description("Carbohydrates per 100g in grams (0-100)")] decimal? CarbsPer100g,
    [property: Description("Fat per 100g in grams (0-100)")] decimal? FatPer100g,
    [property: Description("Fiber per 100g in grams (0-100)")] decimal? FiberPer100g,
    [property: Description("Default measurement unit: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece")] string DefaultUnit = "g",
    [property: Description("Density in g/ml (required for accurate volume unit conversion)")] decimal? DensityGramsPerMl = null,
    [property: Description("Weight of one piece in grams (required when unit is 'piece')")] decimal? GramPerPiece = null
) : IProductRequest;

/// <summary>
/// Request body for updating an existing product.
/// </summary>
public record UpdateProductRequest(
    [property: Description("Product name (must be unique, max 200 chars)")] string Name,
    [property: Description("Calories per 100g (0-9000)")] decimal? CaloriesPer100g,
    [property: Description("Protein per 100g in grams (0-100)")] decimal? ProteinPer100g,
    [property: Description("Carbohydrates per 100g in grams (0-100)")] decimal? CarbsPer100g,
    [property: Description("Fat per 100g in grams (0-100)")] decimal? FatPer100g,
    [property: Description("Fiber per 100g in grams (0-100)")] decimal? FiberPer100g,
    [property: Description("Default measurement unit: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece")] string DefaultUnit = "g",
    [property: Description("Density in g/ml (required for accurate volume unit conversion)")] decimal? DensityGramsPerMl = null,
    [property: Description("Weight of one piece in grams (required when unit is 'piece')")] decimal? GramPerPiece = null
) : IProductRequest;

/// <summary>
/// Product details returned from the API.
/// </summary>
public record ProductResponse(
    [property: Description("Unique product identifier")] Guid Id,
    [property: Description("Product name")] string Name,
    [property: Description("Calories per 100g")] decimal? CaloriesPer100g,
    [property: Description("Protein per 100g in grams")] decimal? ProteinPer100g,
    [property: Description("Carbohydrates per 100g in grams")] decimal? CarbsPer100g,
    [property: Description("Fat per 100g in grams")] decimal? FatPer100g,
    [property: Description("Fiber per 100g in grams")] decimal? FiberPer100g,
    [property: Description("Default measurement unit")] string DefaultUnit,
    [property: Description("Density in g/ml for volume conversions")] decimal? DensityGramsPerMl,
    [property: Description("Weight of one piece in grams")] decimal? GramPerPiece,
    [property: Description("User ID of the product creator")] string CreatedByUserId,
    [property: Description("Creation timestamp (UTC)")] DateTime CreatedAt,
    [property: Description("Last update timestamp (UTC)")] DateTime? UpdatedAt,
    [property: Description("Whether the current user owns this product")] bool IsOwner
)
{
    public static ProductResponse FromEntity(Product product, string currentUserId)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.CaloriesPer100g,
            product.ProteinPer100g,
            product.CarbsPer100g,
            product.FatPer100g,
            product.FiberPer100g,
            product.DefaultUnit,
            product.DensityGramsPerMl,
            product.GramPerPiece,
            product.CreatedByUserId,
            product.CreatedAt,
            product.UpdatedAt,
            product.CreatedByUserId == currentUserId
        );
    }
}
