namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Queries.GetProductById;
using DietPlanner.Application.Queries.GetRecipeById;
using DietPlanner.Application.Queries.SearchProducts;
using DietPlanner.Application.Queries.SearchRecipes;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Read-only product and recipe catalog: what the caller may see in the app (their own items and
/// items shared with their household), through the same queries as REST.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.ReadScope)]
internal sealed class CatalogTools
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    [McpServerTool(Name = "search_products", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Searches products the caller can see by name. Nutrition is per 100 g; null means unknown, never zero.")]
    public static async Task<CatalogPageDto<CatalogProductDto>> SearchProducts(
        McpCaller caller,
        IQueryDispatcher dispatcher,
        [Description("Text the name must contain; omit to list everything.")] string? query = null,
        [Description("Only products the caller created.")] bool onlyMine = false,
        [Description("1-based page number.")] int page = 1,
        [Description("Items per page, 1 to 50.")] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        EnsurePage(page, pageSize);
        PagedList<ProductDto> result = await dispatcher.SendAsync<SearchProductsQuery, PagedList<ProductDto>>(
            new SearchProductsQuery(query, onlyMine, caller.AuthSubject(), page, pageSize), ct);
        return new CatalogPageDto<CatalogProductDto>(
            [.. result.Items.Select(CatalogProductDto.From)], result.TotalCount, result.Page, result.PageSize, result.HasNextPage);
    }

    [McpServerTool(Name = "get_product", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns one product the caller can see. Nutrition is per 100 g; null means unknown, never zero.")]
    public static async Task<CatalogProductDto> GetProduct(
        McpCaller caller,
        IQueryDispatcher dispatcher,
        [Description("Product id from search_products or a recipe ingredient.")] Guid productId,
        CancellationToken ct = default)
    {
        ProductDto? product = await dispatcher.SendAsync<GetProductByIdQuery, ProductDto?>(
            new GetProductByIdQuery(productId, caller.AuthSubject()), ct);
        return product is null
            ? throw new McpException($"Product {productId} does not exist or is not visible to you.")
            : CatalogProductDto.From(product);
    }

    [McpServerTool(Name = "search_recipes", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Searches recipes the caller can see by name, with servings, ingredients and nutrition per serving.")]
    public static async Task<CatalogPageDto<CatalogRecipeDto>> SearchRecipes(
        McpCaller caller,
        IQueryDispatcher dispatcher,
        [Description("Text the name must contain; omit to list everything.")] string? query = null,
        [Description("Only recipes the caller created.")] bool onlyMine = false,
        [Description("1-based page number.")] int page = 1,
        [Description("Items per page, 1 to 50.")] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        EnsurePage(page, pageSize);
        PagedList<RecipeDto> result = await dispatcher.SendAsync<SearchRecipesQuery, PagedList<RecipeDto>>(
            new SearchRecipesQuery(query, onlyMine, caller.AuthSubject(), page, pageSize), ct);
        return new CatalogPageDto<CatalogRecipeDto>(
            [.. result.Items.Select(CatalogRecipeDto.From)], result.TotalCount, result.Page, result.PageSize, result.HasNextPage);
    }

    [McpServerTool(Name = "get_recipe", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns one recipe the caller can see, with instructions, servings, ingredients and nutrition per serving.")]
    public static async Task<CatalogRecipeDto> GetRecipe(
        McpCaller caller,
        IQueryDispatcher dispatcher,
        [Description("Recipe id from search_recipes.")] Guid recipeId,
        CancellationToken ct = default)
    {
        RecipeDto? recipe = await dispatcher.SendAsync<GetRecipeByIdQuery, RecipeDto?>(
            new GetRecipeByIdQuery(recipeId, caller.AuthSubject()), ct);
        return recipe is null
            ? throw new McpException($"Recipe {recipeId} does not exist or is not visible to you.")
            : CatalogRecipeDto.From(recipe);
    }

    private static void EnsurePage(int page, int pageSize)
    {
        if (page < 1)
            throw new McpException("page must be 1 or greater.");
        if (pageSize is < 1 or > MaxPageSize)
            throw new McpException($"pageSize must be between 1 and {MaxPageSize}.");
    }
}
