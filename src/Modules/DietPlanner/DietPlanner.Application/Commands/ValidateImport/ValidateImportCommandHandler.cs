namespace DietPlanner.Application.Commands.ValidateImport;

using DietPlanner.Domain.Repositories;
using Shared.Abstractions.CQRS;

internal sealed class ValidateImportCommandHandler : ICommandHandler<ValidateImportCommand, ValidationResultDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IRecipeRepository _recipeRepository;

    public ValidateImportCommandHandler(
        IProductRepository productRepository,
        IRecipeRepository recipeRepository)
        => (_productRepository, _recipeRepository) = (productRepository, recipeRepository);

    public async Task<ValidationResultDto> HandleAsync(ValidateImportCommand command, CancellationToken ct = default)
    {
        var issues = new List<ValidationIssueDto>();
        var import = command.Import;
        var userId = command.UserId;

        var products = import.Products ?? [];
        var recipes = import.Recipes ?? [];
        var schedule = import.Schedule ?? [];

        // ── 1. Product validation ───────────────────────────────────────────
        var knownProductNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (p, i) in products.Select((p, i) => (p, i)))
        {
            var path = $"products[{i}]";

            if (string.IsNullOrWhiteSpace(p.Name))
            {
                issues.Add(new ValidationIssueDto("error", "validation", $"{path}.name", null,
                    "Product name is required.", "Provide a non-empty name."));
                continue;
            }

            knownProductNames.Add(p.Name);

            if (p.CaloriesPer100g is < 0)
                issues.Add(new ValidationIssueDto("warning", "validation", $"{path}.caloriesPer100g", p.Name,
                    $"Calories for '{p.Name}' is negative ({p.CaloriesPer100g}).", "Use a value ≥ 0."));
        }

        // Detect duplicate product names in the import payload
        var duplicateProducts = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var name in duplicateProducts)
            issues.Add(new ValidationIssueDto("warning", "conflict", "products", name,
                $"Product '{name}' appears more than once. The last definition will be used.",
                "Remove duplicates."));

        // ── 2. Recipe validation ────────────────────────────────────────────
        var knownRecipeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (r, i) in recipes.Select((r, i) => (r, i)))
        {
            var path = $"recipes[{i}]";

            if (string.IsNullOrWhiteSpace(r.Name))
            {
                issues.Add(new ValidationIssueDto("error", "validation", $"{path}.name", null,
                    "Recipe name is required.", "Provide a non-empty name."));
                continue;
            }

            knownRecipeNames.Add(r.Name);

            foreach (var (ing, j) in (r.Ingredients ?? []).Select((ing, j) => (ing, j)))
            {
                var ingPath = $"{path}.ingredients[{j}]";

                if (string.IsNullOrWhiteSpace(ing.Product))
                    issues.Add(new ValidationIssueDto("error", "referential", $"{ingPath}.product", r.Name,
                        $"Ingredient at index {j} of recipe '{r.Name}' has no product name.",
                        "Specify the product name."));
                else if (!knownProductNames.Contains(ing.Product))
                {
                    // Will try to resolve from DB at import time; flag as info
                    issues.Add(new ValidationIssueDto("info", "referential", $"{ingPath}.product", ing.Product,
                        $"Product '{ing.Product}' is not in the import payload. It will be looked up in the database.",
                        "Ensure the product exists in the catalogue."));
                }
            }
        }

        // ── 3. Schedule validation ──────────────────────────────────────────
        foreach (var (day, i) in schedule.Select((d, i) => (d, i)))
        {
            var path = $"schedule[{i}]";

            if (!DateOnly.TryParse(day.Date, out _))
            {
                issues.Add(new ValidationIssueDto("error", "validation", $"{path}.date", null,
                    $"'{day.Date}' is not a valid date (expected YYYY-MM-DD).",
                    "Use ISO 8601 date format."));
                continue;
            }

            foreach (var (meal, j) in (day.Meals ?? []).Select((m, j) => (m, j)))
            {
                var mealPath = $"{path}.meals[{j}]";

                if (string.IsNullOrWhiteSpace(meal.Recipe))
                    issues.Add(new ValidationIssueDto("error", "referential", $"{mealPath}.recipe", null,
                        $"Meal at index {j} on {day.Date} has no recipe name.",
                        "Specify the recipe name."));
                else if (!knownRecipeNames.Contains(meal.Recipe))
                    issues.Add(new ValidationIssueDto("info", "referential", $"{mealPath}.recipe", meal.Recipe,
                        $"Recipe '{meal.Recipe}' is not in the import payload. It will be looked up in the database.",
                        "Ensure the recipe exists in the catalogue."));
            }
        }

        // ── 4. DB conflict checks ───────────────────────────────────────────
        int productsToCreate = 0, productsToReuse = 0;

        foreach (var p in products.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            var existing = await _productRepository.GetByNameAsync(p.Name, userId, ct);
            if (existing is not null)
            {
                productsToReuse++;
                issues.Add(new ValidationIssueDto("info", "conflict", "products", p.Name,
                    $"Product '{p.Name}' already exists and will be reused.", null));
            }
            else
            {
                productsToCreate++;
            }
        }

        int recipesToCreate = 0, recipesToReuse = 0;

        foreach (var r in recipes.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            var existing = await _recipeRepository.GetByNameAsync(r.Name, userId, ct);
            if (existing is not null)
            {
                recipesToReuse++;
                issues.Add(new ValidationIssueDto("info", "conflict", "recipes", r.Name,
                    $"Recipe '{r.Name}' already exists and will be reused.", null));
            }
            else
            {
                recipesToCreate++;
            }
        }

        int mealEntriesToCreate = schedule.Sum(d => (d.Meals ?? []).Count);

        // ── 5. Build result ─────────────────────────────────────────────────
        int errorCount = issues.Count(i => i.Severity == "error");
        int warningCount = issues.Count(i => i.Severity == "warning");
        int infoCount = issues.Count(i => i.Severity == "info");

        bool valid = errorCount == 0;
        bool canProceed = valid;

        var plan = canProceed
            ? new ImportPlanDto(productsToCreate, productsToReuse, recipesToCreate, recipesToReuse, mealEntriesToCreate)
            : null;

        return new ValidationResultDto(
            Valid: valid,
            CanProceed: canProceed,
            Summary: new ValidationSummaryDto(errorCount, warningCount, infoCount),
            Issues: issues,
            Plan: plan);
    }
}
