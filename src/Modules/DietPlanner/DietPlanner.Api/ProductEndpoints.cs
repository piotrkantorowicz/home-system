namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateProduct;
using DietPlanner.Application.Commands.DeleteProduct;
using DietPlanner.Application.Commands.UpdateProduct;
using DietPlanner.Application.Queries.GetProductById;
using DietPlanner.Application.Queries.SearchProducts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Pagination;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Products")
            .RequireAuthorization();

        group.MapGet("/", ListProducts)
            .WithName("ListProducts")
            .WithSummary("List products with optional search and pagination")
            .WithDescription("Returns a paginated list of products visible to the caller. Use `onlyMine=true` to restrict results to products created by the current user.")
            .Produces<PagedList<ProductDto>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetProduct)
            .WithName("GetProduct")
            .WithSummary("Get a product by ID")
            .WithDescription("Returns full product details including nutritional values and ownership flag. Returns 404 if the product does not exist or is not visible to the caller.")
            .Produces<ProductDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .WithDescription("Creates a new product in the catalogue owned by the current user. All nutritional values are optional and stored as per-100g figures.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithSummary("Update a product")
            .WithDescription("Updates all fields of an existing product. Only the product owner may update it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .WithDescription("Permanently removes a product from the catalogue. Only the product owner may delete it.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> ListProducts(
        [AsParameters] ListProductsParams @params,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await dispatcher.SendAsync<SearchProductsQuery, PagedList<ProductDto>>(
            new SearchProductsQuery(@params.Search, @params.OnlyMine, userId, @params.Page, @params.PageSize), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetProduct(
        Guid id,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        ProductDto? result = await dispatcher.SendAsync<GetProductByIdQuery, ProductDto?>(
            new GetProductByIdQuery(id, userId), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var id = await dispatcher.SendAsync<CreateProductCommand, Guid>(
            new CreateProductCommand(
                request.Name, request.Calories, request.Protein, request.Carbs,
                request.Fat, request.Fiber, request.DefaultUnit,
                request.DensityGramsPerMl, request.GramPerPiece, userId), ct);
        return TypedResults.Created($"/api/v1/products/{id}", id);
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(
            new UpdateProductCommand(
                id, request.Name, request.Calories, request.Protein, request.Carbs,
                request.Fat, request.Fiber, request.DefaultUnit,
                request.DensityGramsPerMl, request.GramPerPiece, userId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteProduct(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new DeleteProductCommand(id, userId), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record ListProductsParams(
    [property: FromQuery] string? Search,
    [property: FromQuery] bool OnlyMine = false,
    [property: FromQuery] int Page = 1,
    [property: FromQuery] int PageSize = 50);

public sealed record CreateProductRequest(
    string Name,
    decimal? Calories,
    decimal? Protein,
    decimal? Carbs,
    decimal? Fat,
    decimal? Fiber,
    string DefaultUnit,
    decimal? DensityGramsPerMl,
    decimal? GramPerPiece);

public sealed record UpdateProductRequest(
    string Name,
    decimal? Calories,
    decimal? Protein,
    decimal? Carbs,
    decimal? Fat,
    decimal? Fiber,
    string DefaultUnit,
    decimal? DensityGramsPerMl,
    decimal? GramPerPiece);
