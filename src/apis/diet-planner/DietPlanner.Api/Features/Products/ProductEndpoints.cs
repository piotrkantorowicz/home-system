using DietPlanner.Api.Common.Extensions;
using DietPlanner.Api.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Features.Products;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Products")
            .RequireRateLimiting("api");

        // GET /api/v1/products - List products with pagination and search
        group.MapGet("/", async (
            HttpContext context,
            [FromServices] IProductService productService,
            [FromQuery] string? search,
            [FromQuery] bool onlyMine,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var userId = context.User.GetUserId();
            var result = await productService.SearchAsync(search, onlyMine, userId, page, pageSize);

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("ListProducts")
        .WithSummary("List all products with optional search and filtering")
        .WithDescription("Returns paginated list of products. Use 'search' to filter by name, 'onlyMine' to see only your products.")
        .Produces<PagedResult<ProductResponse>>(StatusCodes.Status200OK);

        // GET /api/v1/products/{id} - Get product by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IProductService productService,
            HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            var product = await productService.GetByIdAsync(id, userId);

            return Results.Ok(product);
        })
        .RequireAuthorization()
        .WithName("GetProduct")
        .WithSummary("Get a product by ID")
        .Produces<ProductResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/products - Create new product
        group.MapPost("/", async (
            [FromBody] CreateProductRequest request,
            [FromServices] IProductService productService,
            [FromServices] IValidator<CreateProductRequest> validator,
            HttpContext context) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = context.User.GetUserId();
            var product = await productService.CreateAsync(request, userId);

            return Results.Created($"/api/v1/products/{product.Id}", product);
        })
        .RequireAuthorization()
        .WithName("CreateProduct")
        .WithSummary("Create a new product")
        .WithDescription("Creates a new product. Product names must be unique across all users.")
        .Produces<ProductResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /api/v1/products/{id} - Update product
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateProductRequest request,
            [FromServices] IProductService productService,
            [FromServices] IValidator<UpdateProductRequest> validator,
            HttpContext context) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = context.User.GetUserId();
            var product = await productService.UpdateAsync(id, request, userId);

            return Results.Ok(product);
        })
        .RequireAuthorization()
        .WithName("UpdateProduct")
        .WithSummary("Update a product")
        .WithDescription("Updates a product. You can only update products you created.")
        .Produces<ProductResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/v1/products/{id} - Soft delete product
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IProductService productService,
            HttpContext context,
            [FromQuery] bool permanent = false) =>
        {
            var userId = context.User.GetUserId();
            await productService.DeleteAsync(id, userId, permanent);

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("DeleteProduct")
        .WithSummary("Delete a product (soft delete by default)")
        .WithDescription("Deletes a product. Default is soft delete. Use 'permanent=true' for hard delete (dev/test environments only). You can only delete products you created.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
