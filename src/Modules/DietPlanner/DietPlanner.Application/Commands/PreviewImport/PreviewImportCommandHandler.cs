namespace DietPlanner.Application.Commands.PreviewImport;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.ImportPlanning;
using DietPlanner.Application.OperationReceipts;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Cqrs;

internal sealed class PreviewImportCommandHandler(ImportPlanResolver resolver, IProductRepository productRepository)
    : ICommandHandler<PreviewImportCommand, ImportPreviewDto>
{
    public async Task<ImportPreviewDto> HandleAsync(PreviewImportCommand command, CancellationToken ct = default)
    {
        ResolvedImport resolved = await resolver.ResolveAsync(
            command.Import, command.UserId, command.PersonId, strict: true, command.AppendToOccupiedSlots, ct);

        var issues = resolved.Issues;
        var summary = new ValidationSummaryDto(
            issues.Count(i => i.Severity == ImportPlanResolver.Error),
            issues.Count(i => i.Severity == ImportPlanResolver.Warning),
            issues.Count(i => i.Severity == ImportPlanResolver.Info));
        bool canExecute = summary.Errors == 0;

        var newRecipeIds = resolved.Recipes.Where(r => r.IsNew).Select(r => r.Id).ToHashSet();
        var slotOrder = resolved.Schedule?.Slots.ToDictionary(s => s.Name, s => s.SortOrder, StringComparer.OrdinalIgnoreCase) ?? [];
        var days = resolved.Meals
            .GroupBy(m => m.Date)
            .OrderBy(g => g.Key)
            .Select(g => new PreviewDayDto(g.Key, [.. g
                .OrderBy(m => slotOrder.GetValueOrDefault(m.SlotName, int.MaxValue))
                .Select(m => new PreviewMealDto(m.SlotName, m.RecipeName, m.Servings, newRecipeIds.Contains(m.RecipeId)))]))
            .ToList();

        var products = resolved.Products.Select(p => new PreviewCatalogItemDto(p.Definition.Name, p.IsNew, p.IsNew ? null : p.Id.Value)).ToList();
        var recipes = resolved.Recipes.Select(r => new PreviewCatalogItemDto(r.Definition.Name, r.IsNew, r.IsNew ? null : r.Id.Value)).ToList();

        var counts = canExecute
            ? new ImportPlanDto(
                products.Count(p => p.IsNew), products.Count(p => !p.IsNew),
                recipes.Count(r => r.IsNew), recipes.Count(r => !r.IsNew),
                resolved.Meals.Count)
            : null;

        var unknownNutrition = await ProductsWithUnknownNutritionAsync(resolved, ct);

        // Everything execution depends on: who, which policy, which file, and how it resolved today.
        var fingerprint = OperationReplay.Hash(new
        {
            command.UserId,
            command.PersonId,
            command.AppendToOccupiedSlots,
            command.Import,
            Products = products,
            Recipes = recipes,
            // New items get fresh ids on every resolution, so they are identified by name; reused ones by id.
            Meals = resolved.Meals.Select(m => new
            {
                m.Date,
                m.SlotName,
                m.RecipeName,
                ExistingRecipe = newRecipeIds.Contains(m.RecipeId) ? (Guid?)null : m.RecipeId.Value,
                m.Servings,
                m.Notes,
            }),
            Issues = issues,
        });

        return new ImportPreviewDto(canExecute, summary, issues, counts, days, products, recipes, unknownNutrition, fingerprint);
    }

    private async Task<IReadOnlyList<string>> ProductsWithUnknownNutritionAsync(ResolvedImport resolved, CancellationToken ct)
    {
        var plannedRecipeIds = resolved.Meals.Select(m => m.RecipeId).ToHashSet();
        var usedIds = resolved.Recipes
            .Where(r => plannedRecipeIds.Contains(r.Id))
            .SelectMany(r => r.UsedProductIds)
            .ToHashSet();

        var newProducts = resolved.Products.Where(p => p.IsNew).ToDictionary(p => p.Id);
        var unknown = newProducts.Values
            .Where(p => usedIds.Contains(p.Id))
            .Where(p => p.Definition.CaloriesPer100g is null || p.Definition.ProteinPer100g is null
                || p.Definition.CarbsPer100g is null || p.Definition.FatPer100g is null)
            .Select(p => p.Definition.Name)
            .ToList();

        var storedIds = usedIds.Where(id => !newProducts.ContainsKey(id)).ToList();
        if (storedIds.Count > 0)
        {
            unknown.AddRange((await productRepository.GetByIdsAsync(storedIds, ct))
                .Where(p => p.Nutrition.Calories is null || p.Nutrition.Protein is null || p.Nutrition.Carbs is null || p.Nutrition.Fat is null)
                .Select(p => p.Name));
        }

        return [.. unknown.Order(StringComparer.OrdinalIgnoreCase)];
    }
}
