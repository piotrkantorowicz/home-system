namespace DietPlanner.Application.Queries.SearchProducts;

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
