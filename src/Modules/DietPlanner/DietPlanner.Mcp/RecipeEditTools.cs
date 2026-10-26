namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.CreateRecipe;
using DietPlanner.Application.Commands.DeleteRecipe;
using DietPlanner.Application.Commands.UpdateRecipe;
using DietPlanner.Application.Queries.GetRecipeById;
using DietPlanner.Application.Queries.SearchRecipes;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Changes to existing recipes, through the same commands and ownership rules as REST: the creator,
/// or an adult of the creator's household for a shared recipe. Repeating a call has no further
/// effect, so these take no operation key (a repeated delete reports not-found).
/// Full replace without a version check, like REST PUT: last write wins.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class RecipeEditTools
{
    [McpServerTool(Name = "update_recipe", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Replaces every field and the whole ingredient list of a recipe the caller may edit (their own, or a household recipe as an adult of that household), with the same rules as the app. Fields and ingredients you leave out are removed, so read the recipe first with get_recipe and send back what should stay. Ingredient amounts are for the whole recipe; servings says how many portions they make. New ingredients must be products the caller can see. Planned and eaten meals that use the recipe see the new version.")]
    public static async Task<CatalogRecipeDto> UpdateRecipe(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Recipe id from search_recipes or get_recipe.")] Guid recipeId,
        [Description("Recipe name, unique in the caller's catalog.")] string name,
        [Description("Portions the ingredient amounts make; at least 1.")] int servings,
        [Description("The complete ingredient list; at least one line.")] IReadOnlyList<RecipeIngredientInput> ingredients,
        [Description("Short description, if any.")] string? description = null,
        [Description("Preparation steps, if any.")] string? instructions = null,
        [Description("Preparation time in minutes, if known.")] int? prepTimeMinutes = null,
        [Description("Private or Household; omit to keep the current one. Only the creator may change it.")] string? visibility = null,
        CancellationToken ct = default)
    {
        var subject = caller.AuthSubject();
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new UpdateRecipeCommand(recipeId, name, description, instructions, servings, prepTimeMinutes,
                [.. ingredients.Select(i => new CreateRecipeIngredientRequest(i.ProductId, i.Amount, i.Unit))],
                visibility, subject), ct);
            return true;
        });

        RecipeDto recipe = await queries.SendAsync<GetRecipeByIdQuery, RecipeDto?>(new GetRecipeByIdQuery(recipeId, subject), ct)
            ?? throw new McpException($"Recipe {recipeId} is no longer visible to you.");
        return CatalogRecipeDto.From(recipe);
    }

    [McpServerTool(Name = "delete_recipe", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Deletes a recipe the caller may edit (their own, or a household recipe as an adult of that household), with the same rules as the app. It disappears from search and can no longer be planned; meals already planned or eaten keep showing it. There is no undo: confirm with the user first. Repeating the call after a successful delete reports not found; the recipe is already gone.")]
    public static async Task<DeletedItemDto> DeleteRecipe(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Recipe id from search_recipes or get_recipe.")] Guid recipeId,
        CancellationToken ct = default)
    {
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new DeleteRecipeCommand(recipeId, caller.AuthSubject()), ct);
            return true;
        });
        return new DeletedItemDto(recipeId);
    }
}
