namespace DietPlanner.Application.Commands.ValidateImport;

using DietPlanner.Application.ImportPlanning;
using Shared.Abstractions.Cqrs;

internal sealed class ValidateImportCommandHandler(ImportPlanResolver resolver)
    : ICommandHandler<ValidateImportCommand, ValidationResultDto>
{
    public async Task<ValidationResultDto> HandleAsync(ValidateImportCommand command, CancellationToken ct = default)
    {
        ResolvedImport resolved = await resolver.ResolveAsync(
            command.Import, command.UserId, command.PersonId, command.Strict, command.AppendToOccupiedSlots, ct);

        var issues = resolved.Issues;
        int errorCount = issues.Count(i => i.Severity == ImportPlanResolver.Error);
        int warningCount = issues.Count(i => i.Severity == ImportPlanResolver.Warning);
        int infoCount = issues.Count(i => i.Severity == ImportPlanResolver.Info);
        bool valid = errorCount == 0;

        var plan = valid
            ? new ImportPlanDto(
                ProductsToCreate: resolved.Products.Count(p => p.IsNew),
                ProductsToReuse: resolved.Products.Count(p => !p.IsNew),
                RecipesToCreate: resolved.Recipes.Count(r => r.IsNew),
                RecipesToReuse: resolved.Recipes.Count(r => !r.IsNew),
                MealEntriesToCreate: resolved.Meals.Count)
            : null;

        return new ValidationResultDto(
            Valid: valid,
            CanProceed: valid,
            Summary: new ValidationSummaryDto(errorCount, warningCount, infoCount),
            Issues: issues,
            Plan: plan);
    }
}
