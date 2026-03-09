namespace DietPlanner.Api.Features.DietPlans.Import;

public interface IImportExecutor
{
    Task<ImportResultDto> ExecuteAsync(ImportDto import, string userId);
}
