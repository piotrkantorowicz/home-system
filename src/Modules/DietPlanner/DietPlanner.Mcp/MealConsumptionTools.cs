namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.BulkCompleteMealEntries;
using DietPlanner.Application.Commands.CompleteMealEntry;
using DietPlanner.Application.Commands.OverrideMealEntry;
using DietPlanner.Application.Commands.ResetMealEntry;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Records what the caller ate, through the same commands and status transitions as REST:
/// Planned → Done, Planned/Done → Modified, anything → Planned. Every call has no further effect
/// when repeated with the same input, so none takes an operation key.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class MealConsumptionTools
{
    [McpServerTool(Name = "complete_meal", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Marks one meal in the caller's diary as eaten exactly as planned (status Done). Already Done stays Done. A Modified meal is refused: if the user now ate it as planned, call reset_meal first.")]
    public static async Task<MealStatusDto> CompleteMeal(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Meal id from get_meal_entries.")] Guid mealId,
        CancellationToken ct = default)
    {
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new CompleteMealEntryCommand(mealId, caller.PersonId(), caller.AuthSubject()), ct);
            return true;
        });
        return new MealStatusDto(mealId, "Done");
    }

    [McpServerTool(Name = "override_meal", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Records that the caller ate something other than the plan for one meal (status Modified): a replacement recipe, individual products, or both, never neither. Replaces any earlier override of that meal. The day's nutrition then counts what was actually eaten. Recipes and products must be ones the caller can see.")]
    public static async Task<MealStatusDto> OverrideMeal(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Meal id from get_meal_entries.")] Guid mealId,
        [Description("Recipe eaten instead of the planned one, from search_recipes; omit when only products were eaten.")] Guid? actualRecipeId = null,
        [Description("Products eaten; may be empty when a recipe is given.")] IReadOnlyList<EatenProductInput>? actualProducts = null,
        CancellationToken ct = default)
    {
        var products = (actualProducts ?? []).Select(p => new ActualProductInput(p.ProductId, p.Amount, p.Unit)).ToList();
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new OverrideMealEntryCommand(mealId, caller.PersonId(), actualRecipeId, products, caller.AuthSubject()), ct);
            return true;
        });
        return new MealStatusDto(mealId, "Modified");
    }

    [McpServerTool(Name = "reset_meal", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Returns one meal in the caller's diary to Planned, discarding that it was eaten and any override. Already Planned stays Planned.")]
    public static async Task<MealStatusDto> ResetMeal(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Meal id from get_meal_entries.")] Guid mealId,
        CancellationToken ct = default)
    {
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new ResetMealEntryCommand(mealId, caller.PersonId(), caller.AuthSubject()), ct);
            return true;
        });
        return new MealStatusDto(mealId, "Planned");
    }

    [McpServerTool(Name = "complete_day", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Marks every Planned meal of one day in the caller's diary as Done. Done and Modified meals are left as they are. Returns how many meals changed; 0 on a repeat.")]
    public static async Task<CompletedDayDto> CompleteDay(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Calendar day, ISO yyyy-MM-dd.")] DateOnly date,
        CancellationToken ct = default)
    {
        BulkCompleteResult result = await McpErrors.AsToolErrorsAsync(() =>
            commands.SendAsync<BulkCompleteMealEntriesCommand, BulkCompleteResult>(
                new BulkCompleteMealEntriesCommand(caller.PersonId(), date, caller.AuthSubject()), ct));
        return new CompletedDayDto(date, result.Completed);
    }
}
