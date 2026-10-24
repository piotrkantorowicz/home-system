namespace DietPlanner.Application.Commands.PreviewImport;

using DietPlanner.Application.Commands.ValidateImport;

/// <summary>
/// What executing an import would do.
/// </summary>
/// <param name="CanExecute">True when strict resolution found no error.</param>
/// <param name="Summary">Issue counts by severity.</param>
/// <param name="Issues">Every finding with its JSON path.</param>
/// <param name="Counts">Products, recipes and meals created or reused; <see langword="null"/> when it cannot execute.</param>
/// <param name="Days">The planned meals per day, in date and slot order.</param>
/// <param name="Products">Every product of the file and whether it is created or reused.</param>
/// <param name="Recipes">Every recipe of the file and whether it is created or reused.</param>
/// <param name="ProductsWithUnknownNutrition">Products the planned recipes use whose calories or macros are unknown, so nutrition totals for those meals are incomplete.</param>
/// <param name="Fingerprint">Hash of the caller, policy, file and its resolution against current data.</param>
public sealed record ImportPreviewDto(
    bool CanExecute,
    ValidationSummaryDto Summary,
    IReadOnlyList<ValidationIssueDto> Issues,
    ImportPlanDto? Counts,
    IReadOnlyList<PreviewDayDto> Days,
    IReadOnlyList<PreviewCatalogItemDto> Products,
    IReadOnlyList<PreviewCatalogItemDto> Recipes,
    IReadOnlyList<string> ProductsWithUnknownNutrition,
    string Fingerprint);
