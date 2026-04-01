namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateRecipe;
using DietPlanner.Application.Commands.DeleteRecipe;
using DietPlanner.Application.Commands.UpdateRecipe;
using DietPlanner.Application.Queries.GetRecipeById;
using DietPlanner.Application.Queries.SearchRecipes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Pagination;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes")
            .RequireAuthorization();

        group.MapGet("/", ListRecipes)
            .WithName("ListRecipes")
            .WithSummary("List recipes with optional search and pagination")
            .WithDescription("Returns a paginated list of recipes visible to the caller. Use `onlyMine=true` to restrict results to recipes created by the current user.")
            .Produces<PagedList<RecipeDto>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetRecipe)
            .WithName("GetRecipe")
            .WithSummary("Get a recipe by ID")
            .WithDescription("Returns full recipe details including the ingredient list with per-ingredient amounts and units. Returns 404 if the recipe does not exist or is not visible to the caller.")
            .Produces<RecipeDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateRecipe)
            .WithName("CreateRecipe")
            .WithSummary("Create a new recipe")
            .WithDescription("Creates a new recipe owned by the current user. Each ingredient references an existing product by ID. `servings` defines the default portion count used when logging this recipe as a meal entry.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", UpdateRecipe)
            .WithName("UpdateRecipe")
            .WithSummary("Update a recipe")
            .WithDescription("Replaces all fields and the full ingredient list of an existing recipe. Only the recipe owner may update it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", DeleteRecipe)
            .WithName("DeleteRecipe")
            .WithSummary("Delete a recipe")
            .WithDescription("Permanently removes a recipe and its ingredient list. Only the recipe owner may delete it.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> ListRecipes(
        [AsParameters] ListRecipesParams @params,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await dispatcher.SendAsync<SearchRecipesQuery, PagedList<RecipeDto>>(
            new SearchRecipesQuery(@params.Search, @params.OnlyMine, userId, @params.Page, @params.PageSize), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetRecipe(
        Guid id,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        RecipeDto? result = await dispatcher.SendAsync<GetRecipeByIdQuery, RecipeDto?>(
            new GetRecipeByIdQuery(id, userId), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateRecipe(
        CreateRecipeRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var ingredients = request.Ingredients
            .Select(i => new CreateRecipeIngredientRequest(i.ProductId, i.Amount, i.Unit))
            .ToList();
        var id = await dispatcher.SendAsync<CreateRecipeCommand, Guid>(
            new CreateRecipeCommand(
                request.Name, request.Description, request.Instructions,
                request.Servings, request.PrepTimeMinutes, ingredients, userId), ct);
        return TypedResults.Created($"/api/v1/recipes/{id}", id);
    }

    private static async Task<IResult> UpdateRecipe(
        Guid id,
        UpdateRecipeRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var ingredients = request.Ingredients
            .Select(i => new CreateRecipeIngredientRequest(i.ProductId, i.Amount, i.Unit))
            .ToList();
        await dispatcher.SendAsync(
            new UpdateRecipeCommand(
                id, request.Name, request.Description, request.Instructions,
                request.Servings, request.PrepTimeMinutes, ingredients, userId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteRecipe(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new DeleteRecipeCommand(id, userId), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record ListRecipesParams(
    [property: FromQuery] string? Search,
    [property: FromQuery] bool OnlyMine = false,
    [property: FromQuery] int Page = 1,
    [property: FromQuery] int PageSize = 50);

public sealed record RecipeIngredientRequest(
    Guid ProductId,
    decimal Amount,
    string Unit);

public sealed record CreateRecipeRequest(
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients);

public sealed record UpdateRecipeRequest(
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients);
