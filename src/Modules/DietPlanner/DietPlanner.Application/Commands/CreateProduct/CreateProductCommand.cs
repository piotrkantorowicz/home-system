namespace DietPlanner.Application.Commands.CreateProduct;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Creates a product owned by the caller and returns its id. The name must be unique among the caller's products.
/// </summary>
/// <param name="Name">Display name; required.</param>
/// <param name="Calories">Energy per 100 g in kcal, if known.</param>
/// <param name="Protein">Protein per 100 g in grams, if known.</param>
/// <param name="Carbs">Carbohydrates per 100 g in grams, if known.</param>
/// <param name="Fat">Fat per 100 g in grams, if known.</param>
/// <param name="Fiber">Fibre per 100 g in grams, if known.</param>
/// <param name="DefaultUnit">Unit proposed when the product is used: <c>g</c>, <c>ml</c> or <c>piece</c>.</param>
/// <param name="DensityGramsPerMl">Grams per millilitre, needed for volume units.</param>
/// <param name="GramPerPiece">Grams per piece, needed for the <c>piece</c> unit.</param>
/// <param name="Visibility"><c>Private</c>, <c>Household</c> or <c>Public</c>; <see langword="null"/> means <c>Household</c>.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record CreateProductCommand(
    string Name,
    decimal? Calories,
    decimal? Protein,
    decimal? Carbs,
    decimal? Fat,
    decimal? Fiber,
    string DefaultUnit,
    decimal? DensityGramsPerMl,
    decimal? GramPerPiece,
    string? Visibility,
    string UserId) : ICommand<Guid>;
