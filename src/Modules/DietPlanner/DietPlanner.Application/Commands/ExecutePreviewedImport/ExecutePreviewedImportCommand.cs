namespace DietPlanner.Application.Commands.ExecutePreviewedImport;

using DietPlanner.Application.Commands.ValidateImport;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Executes a previewed import in strict mode, atomically and retry-safe. A retry with the same
/// operation key and request returns the original result, even after a restart. A first run
/// re-resolves the plan, writes only if the result still matches the previewed fingerprint, and
/// commits products, recipes, planned meals and the operation receipt together.
/// </summary>
/// <param name="Import">The parsed import file, as previewed.</param>
/// <param name="UserId">Auth subject of the importing user.</param>
/// <param name="PersonId">Person identifier of the importing user.</param>
/// <param name="AppendToOccupiedSlots">The collision policy the preview used.</param>
/// <param name="ExpectedFingerprint">Fingerprint from the preview; <see langword="null"/> when no valid preview is at hand, which only allows replaying an earlier run.</param>
/// <param name="OperationKey">Caller-chosen idempotency key for this import.</param>
public sealed record ExecutePreviewedImportCommand(
    ImportDto Import,
    string UserId,
    Guid PersonId,
    bool AppendToOccupiedSlots,
    string? ExpectedFingerprint,
    string OperationKey) : ICommand<ExecutedImportDto>;
