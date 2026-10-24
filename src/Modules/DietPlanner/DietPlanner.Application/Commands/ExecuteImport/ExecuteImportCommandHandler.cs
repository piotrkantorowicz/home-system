namespace DietPlanner.Application.Commands.ExecuteImport;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.ImportPlanning;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ExecuteImportCommandHandler(
    ImportPlanResolver resolver,
    ImportPlanWriter writer,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ExecuteImportCommand, ImportResultDto>
{
    public async Task<ImportResultDto> HandleAsync(ExecuteImportCommand command, CancellationToken ct = default)
    {
        ResolvedImport resolved = await resolver.ResolveAsync(
            command.Import, command.UserId, command.PersonId, command.Strict, command.AppendToOccupiedSlots, ct);

        // Nothing is skipped: an unresolved item rejects the whole import before any write.
        if (resolved.HasErrors)
            throw ImportPlanResolver.Rejection(nameof(ExecuteImportCommand), resolved);

        await writer.StageAsync(resolved, command.UserId, command.PersonId, clock.GetUtcNow().UtcDateTime, ct);
        await unitOfWork.CommitAsync(ct);

        return new ImportResultDto(Message: "Import completed successfully.", Stats: ImportPlanWriter.StatsOf(resolved));
    }
}
