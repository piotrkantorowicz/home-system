namespace DietPlanner.Application.Commands.ExecuteImport;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class ExecuteImportCommandHandler : ICommandHandler<ExecuteImportCommand, ImportResultDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IMealEntryRepository _mealEntryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExecuteImportCommandHandler(
        IProductRepository productRepository,
        IRecipeRepository recipeRepository,
        IMealEntryRepository mealEntryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _recipeRepository = recipeRepository;
        _mealEntryRepository = mealEntryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportResultDto> HandleAsync(ExecuteImportCommand command, CancellationToken ct = default)
    {
        var import = command.Import;
        var userId = command.UserId;

        int productsCreated = 0, productsReused = 0;
        int recipesCreated = 0, recipesReused = 0;
        int mealEntriesCreated = 0;

        // ── 1. Upsert products ──────────────────────────────────────────────
        // Build a name → ProductId map for recipe ingredient resolution
        var productIdByName = new Dictionary<string, ProductId>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in import.Products ?? [])
        {
            if (string.IsNullOrWhiteSpace(p.Name))
                continue;

            var existing = await _productRepository.GetByNameAsync(p.Name, userId, ct);
            if (existing is not null)
            {
                productIdByName[p.Name] = existing.Id;
                productsReused++;
            }
            else
            {
                var id = ProductId.New();
                var nutrition = new NutritionPer100g(p.CaloriesPer100g, p.ProteinPer100g, p.CarbsPer100g, p.FatPer100g, p.FiberPer100g);
                var product = Product.Create(id, p.Name, nutrition, p.Unit ?? "g", p.DensityGramsPerMl, p.GramPerPiece, userId);
                await _productRepository.AddAsync(product, ct);
                productIdByName[p.Name] = id;
                productsCreated++;
            }
        }

        // ── 2. Upsert recipes ───────────────────────────────────────────────
        var recipeIdByName = new Dictionary<string, RecipeId>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in import.Recipes ?? [])
        {
            if (string.IsNullOrWhiteSpace(r.Name))
                continue;

            var existing = await _recipeRepository.GetByNameAsync(r.Name, userId, ct);
            if (existing is not null)
            {
                recipeIdByName[r.Name] = existing.Id;
                recipesReused++;
            }
            else
            {
                var id = RecipeId.New();
                var recipe = Recipe.Create(id, r.Name, r.Description, r.Instructions,
                    r.Servings ?? 1, r.PrepTimeMinutes, userId);

                foreach (var ing in r.Ingredients ?? [])
                {
                    if (string.IsNullOrWhiteSpace(ing.Product))
                        continue;

                    // Resolve product ID — first from this import batch, then from DB
                    if (!productIdByName.TryGetValue(ing.Product, out ProductId? productId))
                    {
                        var dbProduct = await _productRepository.GetByNameAsync(ing.Product, userId, ct);
                        if (dbProduct is null)
                            continue; // Skip unresolvable ingredients
                        productId = dbProduct.Id;
                    }

                    recipe.AddIngredient(RecipeIngredientId.New(), productId, ing.Amount ?? 100m, ing.Unit);
                }

                await _recipeRepository.AddAsync(recipe, ct);
                recipeIdByName[r.Name] = id;
                recipesCreated++;
            }
        }

        // ── 3. Create meal entries ──────────────────────────────────────────
        foreach (var day in import.Schedule ?? [])
        {
            if (!DateOnly.TryParse(day.Date, out DateOnly date))
                continue;

            foreach (var meal in day.Meals ?? [])
            {
                if (string.IsNullOrWhiteSpace(meal.Recipe))
                    continue;

                // Resolve recipe ID — from this import batch or DB
                if (!recipeIdByName.TryGetValue(meal.Recipe, out RecipeId? recipeId))
                {
                    var dbRecipe = await _recipeRepository.GetByNameAsync(meal.Recipe, userId, ct);
                    if (dbRecipe is null)
                        continue; // Skip unresolvable meals
                    recipeId = dbRecipe.Id;
                }

                var entry = MealEntry.Create(
                    MealEntryId.New(),
                    userId,
                    date,
                    Capitalize(meal.Type),
                    recipeId,
                    meal.Servings ?? 1m,
                    meal.Notes,
                    mealTime: null,
                    sequenceOrder: null);

                await _mealEntryRepository.AddAsync(entry, ct);
                mealEntriesCreated++;
            }
        }

        await _unitOfWork.CommitAsync(ct);

        return new ImportResultDto(
            Message: $"Import completed successfully.",
            Stats: new ImportStatsDto(
                ProductsCreated: productsCreated,
                ProductsReused: productsReused,
                RecipesCreated: recipesCreated,
                RecipesReused: recipesReused,
                MealEntriesCreated: mealEntriesCreated));
    }

    private static string Capitalize(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
