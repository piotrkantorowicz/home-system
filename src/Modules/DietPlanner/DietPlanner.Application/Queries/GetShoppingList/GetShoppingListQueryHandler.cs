namespace DietPlanner.Application.Queries.GetShoppingList;

using DietPlanner.Application.Households;
using DietPlanner.Application.Persistence;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetShoppingListQueryHandler(
    IDietPlannerReadDbContext dbContext,
    HouseholdRosterProvider households) : IQueryHandler<GetShoppingListQuery, IReadOnlyList<ShoppingListItemDto>>
{

    public async Task<IReadOnlyList<ShoppingListItemDto>> HandleAsync(
        GetShoppingListQuery query, CancellationToken ct = default)
    {
        // The shopping list is a shared household resource (#223): every member's planned meals.
        HouseholdRoster roster = await households.GetAsync(query.PersonId, query.AuthSubject, ct);
        var personIds = roster.Members.Select(m => m.PersonId).Append(query.PersonId).Distinct().ToList();

        var entries = await dbContext.MealEntries
            .AsNoTracking()
            .Where(me => personIds.Contains(me.PersonId)
                && (query.From == null || me.Date >= query.From)
                && (query.To == null || me.Date <= query.To))
            .Select(me => new EntryProjection
            {
                RecipeId = me.RecipeId,
                Servings = me.Servings
            })
            .ToListAsync(ct);

        if (entries.Count == 0) return [];

        var recipeIds = entries.Select(e => e.RecipeId).Distinct().ToList();

        var recipes = await dbContext.Recipes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => recipeIds.Contains(r.Id))
            .Select(r => new RecipeProjection
            {
                Id = r.Id,
                Servings = r.Servings,
                Ingredients = r.Ingredients
                    .Select(i => new IngredientProjection
                    {
                        ProductId = i.ProductId,
                        Amount = i.Amount,
                        Unit = i.Unit
                    })
                    .ToList()
            })
            .ToDictionaryAsync(r => r.Id, ct);

        var productIds = recipes.Values
            .SelectMany(r => r.Ingredients.Select(i => i.ProductId))
            .Distinct()
            .ToList();

        if (productIds.Count == 0) return [];

        var products = await dbContext.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductProjection
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToDictionaryAsync(p => p.Id, ct);

        var totals = new Dictionary<(ProductId ProductId, string Unit), decimal>();

        foreach (var entry in entries)
        {
            if (!recipes.TryGetValue(entry.RecipeId, out var recipe)) continue;
            if (recipe.Servings <= 0) continue;

            var multiplier = entry.Servings / recipe.Servings;
            if (multiplier <= 0) continue;

            foreach (var ingredient in recipe.Ingredients)
            {
                var key = (ingredient.ProductId, ingredient.Unit);
                var contribution = ingredient.Amount * multiplier;
                totals[key] = totals.TryGetValue(key, out var current)
                    ? current + contribution
                    : contribution;
            }
        }

        return totals
            .Where(kvp => products.ContainsKey(kvp.Key.ProductId))
            .Select(kvp => new ShoppingListItemDto(
                kvp.Key.ProductId.Value,
                products[kvp.Key.ProductId].Name,
                Math.Round(kvp.Value, 2),
                kvp.Key.Unit))
            .OrderBy(item => item.ProductName)
            .ThenBy(item => item.Unit)
            .ToList();
    }

    private sealed class EntryProjection
    {
        public RecipeId RecipeId { get; init; } = default!;
        public decimal Servings { get; init; }
    }

    private sealed class RecipeProjection
    {
        public RecipeId Id { get; init; } = default!;
        public int Servings { get; init; }
        public List<IngredientProjection> Ingredients { get; init; } = [];
    }

    private sealed class IngredientProjection
    {
        public ProductId ProductId { get; init; } = default!;
        public decimal Amount { get; init; }
        public string Unit { get; init; } = default!;
    }

    private sealed class ProductProjection
    {
        public ProductId Id { get; init; } = default!;
        public string Name { get; init; } = default!;
    }
}
