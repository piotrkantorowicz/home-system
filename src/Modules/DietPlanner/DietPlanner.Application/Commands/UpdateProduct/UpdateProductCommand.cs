namespace DietPlanner.Application.Commands.UpdateProduct;

using Shared.Abstractions.Cqrs;

public sealed record UpdateProductCommand(
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
    string UserId) : ICommand;
