namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.CreateMealEntry;
using DietPlanner.Application.Commands.DeleteMealEntry;
using DietPlanner.Application.Commands.UpdateMealEntry;
using DietPlanner.Application.Queries.GetMealEntries;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Single planned meals in the caller's diary, through the same commands and access checks as REST.
/// Planning takes an operation key so a retried call returns the original meal; update and delete
/// have no further effect when repeated.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class MealPlanTools
{
    [McpServerTool(Name = "plan_meal", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plans one meal in the caller's diary: a recipe the caller can see, in one of their meal slots (ids from get_diet_context) on a day. A slot can hold more than one meal; check get_meal_entries first so you do not plan a duplicate. Retry with the same operationKey and fields to get the original meal back instead of a second one; a new meal needs a new key.")]
    public static async Task<DiaryMealDto> PlanMeal(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Idempotency key for this meal, 8-128 characters (a UUID works). Reuse it only to retry this same call.")] string operationKey,
        [Description("Calendar day, ISO yyyy-MM-dd.")] DateOnly date,
        [Description("Meal slot id from get_diet_context.")] Guid slotId,
        [Description("Recipe id from search_recipes or get_recipe.")] Guid recipeId,
        [Description("Servings of the recipe; more than 0, fractions allowed.")] decimal servings,
        [Description("Short note, if any.")] string? notes = null,
        [Description("Time overriding the slot default, HH:mm, if any.")] TimeOnly? mealTime = null,
        [Description("Order among meals in the same slot, if it matters.")] int? sequenceOrder = null,
        CancellationToken ct = default)
    {
        var (personId, subject) = (caller.PersonId(), caller.AuthSubject());
        Guid id = await McpErrors.AsToolErrorsAsync(() => commands.SendAsync<CreateMealEntryCommand, Guid>(
            new CreateMealEntryCommand(personId, date, slotId, recipeId, servings, notes, mealTime, sequenceOrder, subject,
                OperationKey: operationKey), ct));
        return await ReadAsync(queries, personId, subject, id, date, ct);
    }

    [McpServerTool(Name = "update_planned_meal", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Replaces every field of one meal in the caller's diary: day, slot, recipe, servings, note, time and order. Fields you leave out are cleared, so read the meal first with get_meal_entries and send back what should stay. Changing the recipe needs one the caller can see. Whether the meal was eaten is kept; use the consumption tools for that.")]
    public static async Task<DiaryMealDto> UpdatePlannedMeal(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Meal id from get_meal_entries.")] Guid mealId,
        [Description("Calendar day, ISO yyyy-MM-dd.")] DateOnly date,
        [Description("Meal slot id from get_diet_context or get_meal_entries.")] Guid slotId,
        [Description("Recipe id from search_recipes or get_meal_entries.")] Guid recipeId,
        [Description("Servings of the recipe; more than 0, fractions allowed.")] decimal servings,
        [Description("Short note, if any.")] string? notes = null,
        [Description("Time overriding the slot default, HH:mm, if any.")] TimeOnly? mealTime = null,
        [Description("Order among meals in the same slot, if it matters.")] int? sequenceOrder = null,
        CancellationToken ct = default)
    {
        var (personId, subject) = (caller.PersonId(), caller.AuthSubject());
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new UpdateMealEntryCommand(mealId, personId, date, slotId, recipeId, servings, notes,
                mealTime, sequenceOrder, subject), ct);
            return true;
        });
        return await ReadAsync(queries, personId, subject, mealId, date, ct);
    }

    [McpServerTool(Name = "delete_planned_meal", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Removes one meal from the caller's diary, also one already marked as eaten, which drops it from the day's nutrition totals. There is no undo: confirm with the user first. Repeating the call after a successful delete reports not found; the meal is already gone.")]
    public static async Task<DeletedItemDto> DeletePlannedMeal(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Meal id from get_meal_entries.")] Guid mealId,
        CancellationToken ct = default)
    {
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new DeleteMealEntryCommand(mealId, caller.PersonId(), caller.AuthSubject()), ct);
            return true;
        });
        return new DeletedItemDto(mealId);
    }

    // No single-meal query exists, so read the meal's day. A household adult may edit another member's
    // meal by id; that write succeeds but the meal is not in the caller's own diary.
    // ponytail: own diary only, add a personId argument when the agent plans for household members.
    private static async Task<DiaryMealDto> ReadAsync(IQueryDispatcher queries, Guid personId, string subject, Guid id, DateOnly date, CancellationToken ct)
    {
        IReadOnlyList<MealEntryDto> all = await queries.SendAsync<GetMealEntriesQuery, IReadOnlyList<MealEntryDto>>(
            new GetMealEntriesQuery(personId, date, date, subject), ct);
        MealEntryDto meal = all.FirstOrDefault(m => m.Id == id)
            ?? throw new McpException($"Meal {id} was saved, but it is not in your own diary on {date:yyyy-MM-dd}.");
        return DiaryMealDto.From(meal);
    }
}
