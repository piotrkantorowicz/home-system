namespace DietPlanner.Application.Commands.ExecutePreviewedImport;

using DietPlanner.Application.Commands.ValidateImport;

/// <summary>
/// Outcome of an executed import.
/// </summary>
/// <param name="OperationId">Durable id of the import operation.</param>
/// <param name="Stats">What it created and reused.</param>
/// <param name="From">First planned day, or <see langword="null"/> when the plan has no meals.</param>
/// <param name="To">Last planned day, or <see langword="null"/> when the plan has no meals.</param>
/// <param name="CreatedProductIds">Ids of the products it created (at most 250).</param>
/// <param name="CreatedRecipeIds">Ids of the recipes it created (at most 250).</param>
/// <param name="Replayed">True when this call returned an earlier run's result instead of writing.</param>
public sealed record ExecutedImportDto(
    Guid OperationId,
    ImportStatsDto Stats,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<Guid> CreatedProductIds,
    IReadOnlyList<Guid> CreatedRecipeIds,
    bool Replayed);
