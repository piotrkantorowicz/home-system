namespace DietPlanner.Api.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.CreateProduct;
using DietPlanner.Application.Queries.GetProductById;
using DietPlanner.Application.Queries.SearchProducts;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
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
        RequestContext<CallToolRequestParams> context,
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
        var subject = McpCaller.AuthSubject(context);
        Guid id = await McpErrors.AsToolErrorsAsync(() => commands.SendAsync<CreateProductCommand, Guid>(
            new CreateProductCommand(name, caloriesPer100g, proteinPer100g, carbsPer100g, fatPer100g, fiberPer100g,
                unit, densityGramsPerMl, gramPerPiece, visibility, subject, operationKey), ct));

        ProductDto product = await queries.SendAsync<GetProductByIdQuery, ProductDto?>(new GetProductByIdQuery(id, subject), ct)
            ?? throw new McpException($"Product {id} is no longer visible to you.");
        return new CreatedProductDto(product.Id, product.Name, product.Visibility);
    }
}
