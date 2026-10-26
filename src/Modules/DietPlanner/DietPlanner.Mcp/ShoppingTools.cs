namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.ClearShoppingChecks;
using DietPlanner.Application.Commands.SetShoppingCheck;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Ticks on the household's shopping list, through the same commands as REST. A tick belongs to
/// one list range, so the agent must send the same from/to it read the list with.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class ShoppingTools
{
    [McpServerTool(Name = "check_shopping_item", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Ticks (bought) or unticks one row of the household's shopping list. A row is the list's date range plus product and unit, so send the same from and to used with get_shopping_list. Everyone in the household sees the tick. Guests may not change the list.")]
    public static async Task<ShoppingCheckDto> CheckShoppingItem(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("First day of the list's range, as used with get_shopping_list.")] DateOnly from,
        [Description("Last day of the list's range, as used with get_shopping_list.")] DateOnly to,
        [Description("Product id of the row, from get_shopping_list.")] Guid productId,
        [Description("Unit of the row, from get_shopping_list.")] string unit,
        [Description("True to tick as bought, false to untick.")] bool isChecked,
        CancellationToken ct = default)
    {
        DiaryTools.EnsureRange(from, to);
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new SetShoppingCheckCommand(caller.PersonId(), caller.AuthSubject(), from, to, productId, unit, isChecked), ct);
            return true;
        });
        return new ShoppingCheckDto(productId, unit.Trim(), isChecked);
    }

    [McpServerTool(Name = "clear_shopping_checks", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Unticks every row of the household's shopping list for exactly this date range; lists over other ranges keep their ticks. Affects everyone in the household: confirm with the user first. Guests may not change the list.")]
    public static async Task<ClearedShoppingChecksDto> ClearShoppingChecks(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("First day of the list's range, as used with get_shopping_list.")] DateOnly from,
        [Description("Last day of the list's range, as used with get_shopping_list.")] DateOnly to,
        CancellationToken ct = default)
    {
        DiaryTools.EnsureRange(from, to);
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new ClearShoppingChecksCommand(caller.PersonId(), caller.AuthSubject(), from, to), ct);
            return true;
        });
        return new ClearedShoppingChecksDto(from, to);
    }
}
