namespace DietPlanner.Application.Commands.PreviewImport;

using DietPlanner.Application.Commands.ValidateImport;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Resolves an import file in strict mode and describes exactly what executing it would do, without
/// writing anything. The returned fingerprint identifies this resolution: execution recomputes it and
/// refuses to run when anything relevant changed since the preview.
/// </summary>
/// <param name="Import">The parsed import file.</param>
/// <param name="UserId">Auth subject of the importing user.</param>
/// <param name="PersonId">Person identifier of the importing user.</param>
/// <param name="AppendToOccupiedSlots">True allows meals next to already planned ones instead of rejecting them.</param>
public sealed record PreviewImportCommand(
    ImportDto Import,
    string UserId,
    Guid PersonId,
    bool AppendToOccupiedSlots = false) : ICommand<ImportPreviewDto>;
