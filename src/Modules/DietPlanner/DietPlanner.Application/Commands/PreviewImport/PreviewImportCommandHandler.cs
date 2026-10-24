namespace DietPlanner.Application.Commands.PreviewImport;

using DietPlanner.Application.ImportPlanning;
using Shared.Abstractions.Cqrs;

internal sealed class PreviewImportCommandHandler(ImportPlanResolver resolver, ImportPreviewBuilder builder)
    : ICommandHandler<PreviewImportCommand, ImportPreviewDto>
{
    public async Task<ImportPreviewDto> HandleAsync(PreviewImportCommand command, CancellationToken ct = default)
    {
        ResolvedImport resolved = await resolver.ResolveAsync(
            command.Import, command.UserId, command.PersonId, strict: true, command.AppendToOccupiedSlots, ct);
        return await builder.BuildAsync(resolved, command.Import, command.UserId, command.PersonId, command.AppendToOccupiedSlots, ct);
    }
}
