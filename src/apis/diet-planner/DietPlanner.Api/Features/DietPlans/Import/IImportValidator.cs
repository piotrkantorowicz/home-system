namespace DietPlanner.Api.Features.DietPlans.Import;

public interface IImportValidator
{
    Task<ValidationResultDto> ValidateAsync(ImportDto import, string userId);
}
