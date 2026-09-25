namespace DietPlanner.Application.Queries.GetMealEntries;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetMealEntriesQueryHandler
    : IQueryHandler<GetMealEntriesQuery, IReadOnlyList<MealEntryDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetMealEntriesQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<IReadOnlyList<MealEntryDto>> HandleAsync(
        GetMealEntriesQuery query, CancellationToken ct = default)
    {
        // Phase 1 — pull entries + denormalised slot/recipe/actual-recipe/actual-product data.
        var rows = await _dbContext.MealEntries
            .AsNoTracking()
            .Where(me => me.PersonId == query.PersonId
                && (query.From == null || me.Date >= query.From)
                && (query.To == null || me.Date <= query.To))
            .Join(_dbContext.Recipes.AsNoTracking().IgnoreQueryFilters(),
                me => me.RecipeId,
                r => r.Id,
                (me, r) => new { me, RecipeName = r.Name })
            .Join(_dbContext.MealSlots.AsNoTracking(),
                x => x.me.MealSlotId,
                s => s.Id,
                (x, s) => new RowProjection
                {
                    Id = x.me.Id,
                    Date = x.me.Date,
                    MealSlotId = x.me.MealSlotId,
                    SlotName = s.Name,
                    SlotDefaultTime = s.DefaultTime,
                    SlotSortOrder = s.SortOrder,
                    RecipeId = x.me.RecipeId,
                    RecipeName = x.RecipeName,
                    Servings = x.me.Servings,
                    Notes = x.me.Notes,
                    MealTime = x.me.MealTime,
                    SequenceOrder = x.me.SequenceOrder,
                    CreatedAt = x.me.CreatedAt,
                    Status = x.me.Status,
                    ActualRecipeId = x.me.ActualRecipeId,
                    ActualProducts = x.me.ActualProducts
                        .Join(_dbContext.Products.AsNoTracking().IgnoreQueryFilters(),
                            ap => ap.ProductId,
                            p => p.Id,
                            (ap, p) => new ActualProductRow
                            {
                                Id = ap.Id,
                                ProductId = ap.ProductId,
                                ProductName = p.Name,
                                Amount = ap.Amount,
                                Unit = ap.Unit,
                            })
                        .ToList(),
                })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.SlotSortOrder)
            .ThenBy(x => x.SequenceOrder)
            .ToListAsync(ct);

        if (rows.Count == 0) return [];

        // Phase 2 — load ingredients for every recipe involved (planned for Planned/Done,
        // actual for Modified) plus actual-recipe names.
        var actualRecipeIds = rows
            .Where(r => r.ActualRecipeId is not null)
            .Select(r => r.ActualRecipeId!)
            .Distinct()
            .ToList();

        var actualRecipeNames = actualRecipeIds.Count == 0
            ? new Dictionary<RecipeId, string>()
            : await _dbContext.Recipes
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(r => actualRecipeIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name })
                .ToDictionaryAsync(r => r.Id, r => r.Name, ct);

        var recipeIdsForMacros = rows
            .Select(EffectiveRecipeId)
            .Where(id => id is not null)
            .Select(id => id!)
            .Distinct()
            .ToList();

        var recipes = recipeIdsForMacros.Count == 0
            ? new Dictionary<RecipeId, RecipeMacrosSource>()
            : await _dbContext.Recipes
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(r => recipeIdsForMacros.Contains(r.Id))
                .Select(r => new RecipeMacrosSource
                {
                    Id = r.Id,
                    Servings = r.Servings,
                    Ingredients = r.Ingredients
                        .Select(i => new IngredientMacrosSource
                        {
                            ProductId = i.ProductId,
                            Amount = i.Amount,
                            Unit = i.Unit,
                        })
                        .ToList(),
                })
                .ToDictionaryAsync(r => r.Id, ct);

        var productIds = recipes.Values
            .SelectMany(r => r.Ingredients.Select(i => i.ProductId))
            .Concat(rows.SelectMany(r => r.ActualProducts.Select(ap => ap.ProductId)))
            .Distinct()
            .ToList();

        var products = productIds.Count == 0
            ? new Dictionary<ProductId, ProductMacrosSource>()
            : await _dbContext.Products
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new ProductMacrosSource
                {
                    Id = p.Id,
                    Calories = p.Nutrition.Calories,
                    Protein = p.Nutrition.Protein,
                    Carbs = p.Nutrition.Carbs,
                    Fat = p.Nutrition.Fat,
                    Fiber = p.Nutrition.Fiber,
                    DensityGramsPerMl = p.DensityGramsPerMl,
                    GramPerPiece = p.GramPerPiece,
                })
                .ToDictionaryAsync(p => p.Id, ct);

        // Phase 3 — project rows into DTOs with computed macros.
        return rows
            .Select(row => BuildDto(row, actualRecipeNames, recipes, products))
            .ToList();
    }

    private static RecipeId? EffectiveRecipeId(RowProjection row)
        => row.Status == MealEntryStatus.Modified ? row.ActualRecipeId : row.RecipeId;

    private static MealEntryDto BuildDto(
        RowProjection row,
        Dictionary<RecipeId, string> actualRecipeNames,
        IReadOnlyDictionary<RecipeId, RecipeMacrosSource> recipes,
        IReadOnlyDictionary<ProductId, ProductMacrosSource> products)
    {
        decimal calories = 0, protein = 0, carbs = 0, fat = 0, fiber = 0;
        AccumulateRecipe(row, recipes, products,
            ref calories, ref protein, ref carbs, ref fat, ref fiber);
        if (row.Status == MealEntryStatus.Modified)
        {
            AccumulateActualProducts(row, products,
                ref calories, ref protein, ref carbs, ref fat, ref fiber);
        }

        ActualRecipeDto? actualRecipe = null;
        if (row.ActualRecipeId is not null
            && actualRecipeNames.TryGetValue(row.ActualRecipeId, out var name))
        {
            actualRecipe = new ActualRecipeDto(row.ActualRecipeId.Value, name);
        }

        return new MealEntryDto(
            row.Id.Value,
            row.Date,
            row.MealSlotId.Value,
            row.SlotName,
            row.SlotDefaultTime,
            row.SlotSortOrder,
            row.RecipeId.Value,
            row.RecipeName,
            row.Servings,
            row.Notes,
            row.MealTime,
            row.SequenceOrder,
            row.CreatedAt,
            row.Status.ToString(),
            actualRecipe,
            row.ActualProducts
                .Select(ap => new ActualProductDto(
                    ap.Id.Value, ap.ProductId.Value, ap.ProductName, ap.Amount, ap.Unit))
                .ToList(),
            Math.Round(calories, 1),
            Math.Round(protein, 1),
            Math.Round(carbs, 1),
            Math.Round(fat, 1),
            Math.Round(fiber, 1));
    }

    private static void AccumulateRecipe(
        RowProjection row,
        IReadOnlyDictionary<RecipeId, RecipeMacrosSource> recipes,
        IReadOnlyDictionary<ProductId, ProductMacrosSource> products,
        ref decimal calories, ref decimal protein, ref decimal carbs, ref decimal fat, ref decimal fiber)
    {
        var recipeId = EffectiveRecipeId(row);
        if (recipeId is null || !recipes.TryGetValue(recipeId, out var recipe)) return;

        var multiplier = recipe.Servings > 0 ? row.Servings / recipe.Servings : 0;
        if (multiplier == 0) return;

        foreach (var ing in recipe.Ingredients)
        {
            if (!products.TryGetValue(ing.ProductId, out var product)) continue;

            var grams = UnitConverter.ConvertToGrams(
                ing.Amount, ing.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            var factor = grams / 100m * multiplier;

            calories += (product.Calories ?? 0) * factor;
            protein += (product.Protein ?? 0) * factor;
            carbs += (product.Carbs ?? 0) * factor;
            fat += (product.Fat ?? 0) * factor;
            fiber += (product.Fiber ?? 0) * factor;
        }
    }

    private static void AccumulateActualProducts(
        RowProjection row,
        IReadOnlyDictionary<ProductId, ProductMacrosSource> products,
        ref decimal calories, ref decimal protein, ref decimal carbs, ref decimal fat, ref decimal fiber)
    {
        foreach (var ap in row.ActualProducts)
        {
            if (!products.TryGetValue(ap.ProductId, out var product)) continue;

            var grams = UnitConverter.ConvertToGrams(
                ap.Amount, ap.Unit, product.DensityGramsPerMl, product.GramPerPiece);
            var factor = grams / 100m;

            calories += (product.Calories ?? 0) * factor;
            protein += (product.Protein ?? 0) * factor;
            carbs += (product.Carbs ?? 0) * factor;
            fat += (product.Fat ?? 0) * factor;
            fiber += (product.Fiber ?? 0) * factor;
        }
    }

    private sealed class RowProjection
    {
        public MealEntryId Id { get; init; } = default!;
        public DateOnly Date { get; init; }
        public MealSlotId MealSlotId { get; init; } = default!;
        public string SlotName { get; init; } = default!;
        public TimeOnly SlotDefaultTime { get; init; }
        public int SlotSortOrder { get; init; }
        public RecipeId RecipeId { get; init; } = default!;
        public string RecipeName { get; init; } = default!;
        public decimal Servings { get; init; }
        public string? Notes { get; init; }
        public TimeOnly? MealTime { get; init; }
        public int? SequenceOrder { get; init; }
        public DateTime CreatedAt { get; init; }
        public MealEntryStatus Status { get; init; }
        public RecipeId? ActualRecipeId { get; init; }
        public List<ActualProductRow> ActualProducts { get; init; } = [];
    }

    private sealed class ActualProductRow
    {
        public MealEntryActualProductId Id { get; init; } = default!;
        public ProductId ProductId { get; init; } = default!;
        public string ProductName { get; init; } = default!;
        public decimal Amount { get; init; }
        public string Unit { get; init; } = default!;
    }

    private sealed class RecipeMacrosSource
    {
        public RecipeId Id { get; init; } = default!;
        public int Servings { get; init; }
        public List<IngredientMacrosSource> Ingredients { get; init; } = [];
    }

    private sealed class IngredientMacrosSource
    {
        public ProductId ProductId { get; init; } = default!;
        public decimal Amount { get; init; }
        public string Unit { get; init; } = default!;
    }

    private sealed class ProductMacrosSource
    {
        public ProductId Id { get; init; } = default!;
        public decimal? Calories { get; init; }
        public decimal? Protein { get; init; }
        public decimal? Carbs { get; init; }
        public decimal? Fat { get; init; }
        public decimal? Fiber { get; init; }
        public decimal? DensityGramsPerMl { get; init; }
        public decimal? GramPerPiece { get; init; }
    }
}
