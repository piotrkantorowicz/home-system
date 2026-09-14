namespace DietPlanner.Application.Queries.GetShoppingList;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.ValueObjects;
using Household.Contracts.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Cqrs;

internal sealed partial class GetShoppingListQueryHandler
    : IQueryHandler<GetShoppingListQuery, IReadOnlyList<ShoppingListItemDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;
    private readonly IHouseholdQueryService _households;
    private readonly ILogger<GetShoppingListQueryHandler> _logger;

    public GetShoppingListQueryHandler(
        IDietPlannerReadDbContext dbContext,
        IHouseholdQueryService households,
        ILogger<GetShoppingListQueryHandler> logger)
        => (_dbContext, _households, _logger) = (dbContext, households, logger);

    public async Task<IReadOnlyList<ShoppingListItemDto>> HandleAsync(
        GetShoppingListQuery query, CancellationToken ct = default)
    {
        var userIds = await ResolveHouseholdUserIdsAsync(query.UserId, ct);

        var entries = await _dbContext.MealEntries
            .AsNoTracking()
            .Where(me => userIds.Contains(me.UserId)
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

        var recipes = await _dbContext.Recipes
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

        var products = await _dbContext.Products
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

    /// <summary>
    /// The shopping list is a shared household resource (#223): it aggregates the planned
    /// meals of every member. Resolves the caller's household and returns every member's
    /// auth subject (meal entries are still keyed by subject in v1) plus the caller's own.
    /// Falls back to the caller alone when they have no household or the Household module
    /// is unavailable — the list must never fail because of a household lookup.
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolveHouseholdUserIdsAsync(
        string callerUserId, CancellationToken ct)
    {
        try
        {
            var context = await _households.GetHouseholdContextForUserAsync(callerUserId, ct);
            if (context is null)
                return [callerUserId];

            var subjects = context.Members
                .Select(m => m.AuthSubject)
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .Append(callerUserId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return subjects;
        }
        catch (Exception ex)
        {
            LogHouseholdUnresolved(ex);
            return [callerUserId];
        }
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

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Warning,
        Message = "Could not resolve the household for the shopping list; using the caller's own meals only.")]
    private partial void LogHouseholdUnresolved(Exception exception);
}
