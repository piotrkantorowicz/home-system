namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.DeleteProduct;
using DietPlanner.Application.Commands.UpdateProduct;
using DietPlanner.Application.Queries.GetProductById;
using DietPlanner.Application.Queries.SearchProducts;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Changes to existing products, through the same commands and ownership rules as REST: the creator,
/// or an adult of the creator's household for a shared product. Repeating a call has no further
/// effect, so these take no operation key (a repeated delete reports not-found).
/// Full replace without a version check: an app edit between the agent's read and its update is
/// overwritten, the same as two app tabs today.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class ProductEditTools
{
    [McpServerTool(Name = "update_product", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Replaces every field of a product the caller may edit (their own, or a household product as an adult of that household), with the same rules as the app. Fields you leave out become unknown, so read the product first with get_product and send back what should stay. Nutrition is per 100 g; never send zero for unknown. Recipes and meals that use the product see the new values.")]
    public static async Task<CatalogProductDto> UpdateProduct(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Product id from search_products or get_product.")] Guid productId,
        [Description("Product name, unique in the caller's catalog.")] string name,
        [Description("Default unit: g, ml or piece.")] string unit,
        [Description("Energy in kcal per 100 g, if known.")] decimal? caloriesPer100g = null,
        [Description("Protein in grams per 100 g, if known.")] decimal? proteinPer100g = null,
        [Description("Carbohydrates in grams per 100 g, if known.")] decimal? carbsPer100g = null,
        [Description("Fat in grams per 100 g, if known.")] decimal? fatPer100g = null,
        [Description("Fibre in grams per 100 g, if known.")] decimal? fiberPer100g = null,
        [Description("Grams per millilitre, needed to measure it in volume units, if known.")] decimal? densityGramsPerMl = null,
        [Description("Grams per piece, needed for the piece unit, if known.")] decimal? gramPerPiece = null,
        [Description("Private or Household; omit to keep the current one. Only the creator may change it.")] string? visibility = null,
        CancellationToken ct = default)
    {
        var subject = caller.AuthSubject();
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new UpdateProductCommand(productId, name, caloriesPer100g, proteinPer100g, carbsPer100g,
                fatPer100g, fiberPer100g, unit, densityGramsPerMl, gramPerPiece, visibility, subject), ct);
            return true;
        });

        ProductDto product = await queries.SendAsync<GetProductByIdQuery, ProductDto?>(new GetProductByIdQuery(productId, subject), ct)
            ?? throw new McpException($"Product {productId} is no longer visible to you.");
        return CatalogProductDto.From(product);
    }

    [McpServerTool(Name = "delete_product", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Deletes a product the caller may edit (their own, or a household product as an adult of that household), with the same rules as the app. It disappears from search; recipes that use it keep the ingredient line but show it as Unknown and without its nutrition; meals already in the diary keep showing it. There is no undo: confirm with the user first. Repeating the call after a successful delete reports not found; the product is already gone.")]
    public static async Task<DeletedItemDto> DeleteProduct(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Product id from search_products or get_product.")] Guid productId,
        CancellationToken ct = default)
    {
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new DeleteProductCommand(productId, caller.AuthSubject()), ct);
            return true;
        });
        return new DeletedItemDto(productId);
    }
}
