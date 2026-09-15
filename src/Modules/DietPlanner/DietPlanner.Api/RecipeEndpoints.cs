namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateRecipe;
using DietPlanner.Application.Commands.DeleteRecipe;
using DietPlanner.Application.Commands.UpdateRecipe;
using DietPlanner.Application.Queries.GetRecipeById;
using DietPlanner.Application.Queries.SearchRecipes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Endpoints for recipes (<c>/api/v1/recipes</c>).
/// </summary>
public static class RecipeEndpoints
{
    /// <summary>Maps recipe search, detail, create, update and delete; all require an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes")
            .RequireAuthorization();

        group.MapGet("/", ListRecipes)
            .WithName("ListRecipes")
            .WithSummary("List recipes with optional search and pagination")
            .WithDescription("Returns a paginated list of recipes visible to the caller. Use `onlyMine=true` to restrict results to recipes created by the current user.");

        group.MapGet("/{id:guid}", GetRecipe)
            .WithName("GetRecipe")
            .WithSummary("Get a recipe by ID")
            .WithDescription("Returns full recipe details including the ingredient list with per-ingredient amounts and units. Returns 404 if the recipe does not exist or is not visible to the caller.");

        group.MapPost("/", CreateRecipe)
            .WithName("CreateRecipe")
            .WithSummary("Create a new recipe")
            .WithDescription("Creates a new recipe owned by the current user. Each ingredient references an existing product by ID. `servings` defines the default portion count used when logging this recipe as a meal entry.");

        group.MapPut("/{id:guid}", UpdateRecipe)
            .WithName("UpdateRecipe")
            .WithSummary("Update a recipe")
            .WithDescription("Replaces all fields and the full ingredient list of an existing recipe. Only the recipe owner may update it.");

        group.MapDelete("/{id:guid}", DeleteRecipe)
            .WithName("DeleteRecipe")
            .WithSummary("Delete a recipe")
            .WithDescription("Permanently removes a recipe and its ingredient list. Only the recipe owner may delete it.");

        return app;
    }

    private static async Task<Ok<PagedList<RecipeDto>>> ListRecipes(
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

    private static async Task<Results<Ok<RecipeDto>, NotFound>> GetRecipe(
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

    private static async Task<Created<Guid>> CreateRecipe(
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

    private static async Task<NoContent> UpdateRecipe(
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

    private static async Task<NoContent> DeleteRecipe(
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

/// <summary>
/// Query-string parameters of the recipe search.
/// </summary>
/// <param name="Search">Case-insensitive substring to match against the name.</param>
/// <param name="OnlyMine">When true, only items the caller created.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
public sealed record ListRecipesParams(
    [property: FromQuery] string? Search,
    [property: FromQuery] bool OnlyMine = false,
    [property: FromQuery] int Page = 1,
    [property: FromQuery] int PageSize = 50);

/// <summary>
/// One ingredient line in a recipe create or update.
/// </summary>
/// <param name="ProductId">The product used.</param>
/// <param name="Amount">Quantity for the full recipe; positive.</param>
/// <param name="Unit">Unit of the amount, e.g. <c>g</c>, <c>ml</c>, <c>cup</c>, <c>piece</c>.</param>
public sealed record RecipeIngredientRequest(
    Guid ProductId,
    decimal Amount,
    string Unit);

/// <summary>
/// Body of recipe creation.
/// </summary>
/// <param name="Name">Display name; required and unique per user.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Instructions">Optional preparation steps.</param>
/// <param name="Servings">Portions the ingredient amounts yield; positive.</param>
/// <param name="PrepTimeMinutes">Optional preparation time.</param>
/// <param name="Ingredients">The complete ingredient list.</param>
public sealed record CreateRecipeRequest(
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients);

/// <summary>
/// Body of recipe update; header fields and the whole ingredient list are replaced.
/// </summary>
/// <param name="Name">Display name; required and unique per user.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Instructions">Optional preparation steps.</param>
/// <param name="Servings">Portions the ingredient amounts yield; positive.</param>
/// <param name="PrepTimeMinutes">Optional preparation time.</param>
/// <param name="Ingredients">The complete ingredient list.</param>
public sealed record UpdateRecipeRequest(
    string Name,
    string? Description,
    string? Instructions,
    int Servings,
    int? PrepTimeMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients);
