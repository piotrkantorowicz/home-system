namespace DietPlanner.Application.Commands.ExecutePreviewedImport;

using DietPlanner.Application.Commands.ValidateImport;

/// <summary>
/// Outcome of an executed import.
/// </summary>
/// <param name="OperationId">Durable id of the import operation.</param>
/// <param name="Stats">What it created and reused.</param>
/// <param name="Replayed">True when this call returned an earlier run's result instead of writing.</param>
public sealed record ExecutedImportDto(Guid OperationId, ImportStatsDto Stats, bool Replayed);
