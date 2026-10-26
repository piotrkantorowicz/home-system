namespace DietPlanner.Mcp;

using DietPlanner.Application.Queries.SearchProducts;

/// <summary>
/// A product visible to the caller. Nutrition is per 100 g; <see langword="null"/> means unknown, never zero.
/// </summary>
/// <param name="Id">Product id, for recipe ingredients.</param>
/// <param name="Name">Display name; also the import reuse key.</param>
/// <param name="CaloriesPer100g">Energy in kcal per 100 g, if known.</param>
/// <param name="ProteinPer100g">Protein in grams per 100 g, if known.</param>
/// <param name="CarbsPer100g">Carbohydrates in grams per 100 g, if known.</param>
/// <param name="FatPer100g">Fat in grams per 100 g, if known.</param>
/// <param name="FiberPer100g">Fibre in grams per 100 g, if known.</param>
/// <param name="DefaultUnit">Default unit (<c>g</c>, <c>ml</c>, <c>piece</c>).</param>
/// <param name="DensityGramsPerMl">Grams per millilitre, used for volume units, if known.</param>
/// <param name="GramPerPiece">Grams per piece, used for the <c>piece</c> unit, if known.</param>
/// <param name="IsOwner">True when the caller created it.</param>
/// <param name="Visibility"><c>Private</c> or <c>Household</c>.</param>
public sealed record CatalogProductDto(
    Guid Id,
    string Name,
    decimal? CaloriesPer100g,
    decimal? ProteinPer100g,
    decimal? CarbsPer100g,
    decimal? FatPer100g,
    decimal? FiberPer100g,
    string DefaultUnit,
    decimal? DensityGramsPerMl,
    decimal? GramPerPiece,
    bool IsOwner,
    string Visibility)
{
    internal static CatalogProductDto From(ProductDto p)
        => new(p.Id, p.Name, p.Calories, p.Protein, p.Carbs, p.Fat, p.Fiber, p.DefaultUnit, p.DensityGramsPerMl, p.GramPerPiece, p.IsOwner, p.Visibility);
}
