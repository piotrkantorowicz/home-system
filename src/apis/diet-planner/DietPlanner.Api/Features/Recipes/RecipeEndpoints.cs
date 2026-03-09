using DietPlanner.Api.Common.Extensions;
using DietPlanner.Api.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Features.Recipes;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes")
            .RequireRateLimiting("api");

        // GET /api/v1/recipes - List recipes with pagination and search
        group.MapGet("/", async (
            HttpContext context,
            [FromServices] IRecipeService recipeService,
            [FromQuery] string? search,
            [FromQuery] bool onlyMine,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var userId = context.User.GetUserId();
            var result = await recipeService.SearchAsync(search, onlyMine, userId, page, pageSize);

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("ListRecipes")
        .WithSummary("List all recipes with optional search and filtering")
        .WithDescription("Returns paginated list of recipes with ingredients and nutrition information.")
        .Produces<PagedResult<RecipeResponse>>(StatusCodes.Status200OK);

        // GET /api/v1/recipes/{id} - Get recipe by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IRecipeService recipeService,
            HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            var recipe = await recipeService.GetByIdAsync(id, userId);

            return Results.Ok(recipe);
        })
        .RequireAuthorization()
        .WithName("GetRecipe")
        .WithSummary("Get a recipe by ID")
        .WithDescription("Returns recipe details including ingredients and calculated nutrition information.")
        .Produces<RecipeResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/recipes - Create new recipe
        group.MapPost("/", async (
            [FromBody] CreateRecipeRequest request,
            [FromServices] IRecipeService recipeService,
            [FromServices] IValidator<CreateRecipeRequest> validator,
            HttpContext context) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = context.User.GetUserId();
            var recipe = await recipeService.CreateAsync(request, userId);

            return Results.Created($"/api/v1/recipes/{recipe.Id}", recipe);
        })
        .RequireAuthorization()
        .WithName("CreateRecipe")
        .WithSummary("Create a new recipe")
        .WithDescription("Creates a new recipe with ingredients. All products must exist before creating the recipe.")
        .Produces<RecipeResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /api/v1/recipes/{id} - Update recipe
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateRecipeRequest request,
            [FromServices] IRecipeService recipeService,
            [FromServices] IValidator<UpdateRecipeRequest> validator,
            HttpContext context) =>
        {
            // Validate request
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userId = context.User.GetUserId();
            var recipe = await recipeService.UpdateAsync(id, request, userId);

            return Results.Ok(recipe);
        })
        .RequireAuthorization()
        .WithName("UpdateRecipe")
        .WithSummary("Update a recipe")
        .WithDescription("Updates a recipe and its ingredients. You can only update recipes you created.")
        .Produces<RecipeResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/v1/recipes/{id} - Soft delete recipe
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IRecipeService recipeService,
            HttpContext context,
            [FromQuery] bool permanent = false) =>
        {
            var userId = context.User.GetUserId();
            await recipeService.DeleteAsync(id, userId, permanent);

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("DeleteRecipe")
        .WithSummary("Delete a recipe (soft delete by default)")
        .WithDescription("Deletes a recipe. Default is soft delete. Use 'permanent=true' for hard delete (dev/test environments only). You can only delete recipes you created.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
