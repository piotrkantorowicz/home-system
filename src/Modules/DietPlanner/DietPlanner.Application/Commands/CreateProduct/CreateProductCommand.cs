namespace DietPlanner.Application.Commands.CreateProduct;

using Shared.Abstractions.Cqrs;

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
    string UserId) : ICommand<Guid>;
