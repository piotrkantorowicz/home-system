namespace DietPlanner.Application.Queries.SearchProducts;

/// <summary>
/// A product as shown in lists and detail views; nutrition is per 100 g.
/// </summary>
/// <param name="Id">Identifier of the product.</param>
/// <param name="Name">Display name.</param>
/// <param name="Calories">Energy per 100 g in kcal, if known.</param>
/// <param name="Protein">Protein per 100 g in grams, if known.</param>
/// <param name="Carbs">Carbohydrates per 100 g in grams, if known.</param>
/// <param name="Fat">Fat per 100 g in grams, if known.</param>
/// <param name="Fiber">Fibre per 100 g in grams, if known.</param>
/// <param name="DefaultUnit">Unit proposed when the product is used.</param>
/// <param name="DensityGramsPerMl">Grams per millilitre, if known.</param>
/// <param name="GramPerPiece">Grams per piece, if known.</param>
/// <param name="CreatedByUserId">Auth subject of the creator.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC; <see langword="null"/> if never changed.</param>
/// <param name="IsOwner">Whether the caller created it and may edit or delete it.</param>
public sealed record ProductDto(
    Guid Id,
    string Name,
    decimal? Calories,
    decimal? Protein,
    decimal? Carbs,
    decimal? Fat,
    decimal? Fiber,
    string DefaultUnit,
    decimal? DensityGramsPerMl,
    decimal? GramPerPiece,
    string CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsOwner);
