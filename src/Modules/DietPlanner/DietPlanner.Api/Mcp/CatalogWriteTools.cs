namespace DietPlanner.Api.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.CreateProduct;
using DietPlanner.Application.Commands.CreateRecipe;
using DietPlanner.Application.Queries.GetProductById;
using DietPlanner.Application.Queries.GetRecipeById;
using DietPlanner.Application.Queries.SearchProducts;
using DietPlanner.Application.Queries.SearchRecipes;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Catalog writes. Every write takes a caller-chosen operation key so a retried call returns the
/// original result instead of writing twice.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class CatalogWriteTools
{
    [McpServerTool(Name = "create_product", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Saves a new product in the caller's catalog, with the same rules as the app. Nutrition is per 100 g; leave out values you do not know, never send zero for unknown. Retry with the same operationKey and fields to get the original product back instead of a duplicate; a new product needs a new key.")]
    public static async Task<CreatedProductDto> CreateProduct(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Idempotency key for this product, 8-128 characters (a UUID works). Reuse it only to retry this same call.")] string operationKey,
        [Description("Product name, unique in the caller's catalog.")] string name,
        [Description("Default unit: g, ml or piece.")] string unit,
        [Description("Energy in kcal per 100 g, if known.")] decimal? caloriesPer100g = null,
        [Description("Protein in grams per 100 g, if known.")] decimal? proteinPer100g = null,
        [Description("Carbohydrates in grams per 100 g, if known.")] decimal? carbsPer100g = null,
        [Description("Fat in grams per 100 g, if known.")] decimal? fatPer100g = null,
        [Description("Fibre in grams per 100 g, if known.")] decimal? fiberPer100g = null,
        [Description("Grams per millilitre, needed to measure it in volume units, if known.")] decimal? densityGramsPerMl = null,
        [Description("Grams per piece, needed for the piece unit, if known.")] decimal? gramPerPiece = null,
        [Description("Private or Household; Household when omitted.")] string? visibility = null,
        CancellationToken ct = default)
    {
        var subject = caller.AuthSubject();
        Guid id = await McpErrors.AsToolErrorsAsync(() => commands.SendAsync<CreateProductCommand, Guid>(
            new CreateProductCommand(name, caloriesPer100g, proteinPer100g, carbsPer100g, fatPer100g, fiberPer100g,
                unit, densityGramsPerMl, gramPerPiece, visibility, subject, operationKey), ct));

        ProductDto product = await queries.SendAsync<GetProductByIdQuery, ProductDto?>(new GetProductByIdQuery(id, subject), ct)
            ?? throw new McpException($"Product {id} is no longer visible to you.");
        return new CreatedProductDto(product.Id, product.Name, product.Visibility);
    }

    [McpServerTool(Name = "create_recipe", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Saves a new recipe in the caller's catalog from products the caller can see, with the same rules as the app. Ingredient amounts are for the whole recipe; servings says how many portions they make. Retry with the same operationKey and fields to get the original recipe back instead of a duplicate; a new recipe needs a new key.")]
    public static async Task<CreatedRecipeDto> CreateRecipe(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Idempotency key for this recipe, 8-128 characters (a UUID works). Reuse it only to retry this same call.")] string operationKey,
        [Description("Recipe name, unique in the caller's catalog.")] string name,
        [Description("Portions the ingredient amounts make; at least 1.")] int servings,
        [Description("Ingredient lines; at least one.")] IReadOnlyList<RecipeIngredientInput> ingredients,
        [Description("Short description, if any.")] string? description = null,
        [Description("Preparation steps, if any.")] string? instructions = null,
        [Description("Preparation time in minutes, if known.")] int? prepTimeMinutes = null,
        [Description("Private or Household; Household when omitted.")] string? visibility = null,
        CancellationToken ct = default)
    {
        var subject = caller.AuthSubject();
        Guid id = await McpErrors.AsToolErrorsAsync(() => commands.SendAsync<CreateRecipeCommand, Guid>(
            new CreateRecipeCommand(name, description, instructions, servings, prepTimeMinutes,
                [.. ingredients.Select(i => new CreateRecipeIngredientRequest(i.ProductId, i.Amount, i.Unit))],
                visibility, subject, operationKey), ct));

        RecipeDto recipe = await queries.SendAsync<GetRecipeByIdQuery, RecipeDto?>(new GetRecipeByIdQuery(id, subject), ct)
            ?? throw new McpException($"Recipe {id} is no longer visible to you.");
        return new CreatedRecipeDto(recipe.Id, recipe.Name, recipe.Visibility);
    }
}
