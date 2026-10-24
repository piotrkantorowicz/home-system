namespace DietPlanner.Application.Commands.ExecutePreviewedImport;

using System.Text.Json;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.ImportPlanning;
using DietPlanner.Application.OperationReceipts;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ExecutePreviewedImportCommandHandler(
    ImportPlanResolver resolver,
    ImportPreviewBuilder previews,
    ImportPlanWriter writer,
    OperationReplay replay,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ExecutePreviewedImportCommand, ExecutedImportDto>
{
    private const string Operation = "execute_plan_import";

    public async Task<ExecutedImportDto> HandleAsync(ExecutePreviewedImportCommand command, CancellationToken ct = default)
    {
        ResolvedImport resolved = await resolver.ResolveAsync(
            command.Import, command.UserId, command.PersonId, strict: true, command.AppendToOccupiedSlots, ct);

        var now = clock.GetUtcNow().UtcDateTime;
        var operationId = Guid.CreateVersion7();
        var stats = ImportPlanWriter.StatsOf(resolved);

        // Replay comes first: after a successful run the same plan no longer resolves cleanly (its
        // slots are now occupied), yet a retry must still get the original result.
        var earlier = await replay.ClaimOrReplayReceiptAsync(
            command.UserId, Operation, command.OperationKey,
            new { command.Import, command.AppendToOccupiedSlots },
            operationId, JsonSerializer.Serialize(stats), now, ct);
        if (earlier is not null)
            return new ExecutedImportDto(earlier.ResultId, JsonSerializer.Deserialize<ImportStatsDto>(earlier.Result!)!, Replayed: true);

        // From here on any failure rolls the claimed receipt back with everything else.
        if (command.ExpectedFingerprint is null)
            throw new ConflictException("A valid preview is required to import this plan; preview it again.");

        if (resolved.HasErrors)
            throw ImportPlanResolver.Rejection(nameof(ExecutePreviewedImportCommand), resolved);

        var preview = await previews.BuildAsync(resolved, command.Import, command.UserId, command.PersonId, command.AppendToOccupiedSlots, ct);
        if (preview.Fingerprint != command.ExpectedFingerprint)
            throw new ConflictException("The plan, its options or the data it depends on changed since the preview; preview the plan again.");

        await writer.StageAsync(resolved, command.UserId, command.PersonId, now, ct);
        await unitOfWork.CommitAsync(ct);

        return new ExecutedImportDto(operationId, stats, Replayed: false);
    }
}
