namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.DeleteWaterIntake;
using DietPlanner.Application.Commands.LogWaterIntake;
using DietPlanner.Application.Commands.UpdateHydrationConfig;
using DietPlanner.Application.Queries.GetHydrationConfig;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Changes the caller's hydration settings and drink log, through the same commands as REST.
/// Logging appends, so it takes an operation key; the settings are one record, replaced on each call.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class HydrationTools
{
    [McpServerTool(Name = "update_hydration_config", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Saves the caller's hydration settings, creating them on first use and replacing all three values afterwards. Changes settings only; logged drinks stay as they are.")]
    public static async Task<HydrationConfigDto> UpdateHydrationConfig(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Daily water target in millilitres, 100-10000.")] int dailyWaterTargetMl,
        [Description("Size of one glass in millilitres, 50-2000.")] int glassSizeMl,
        [Description("Whether the app tracks water intake for the caller.")] bool trackWaterIntake,
        CancellationToken ct = default)
    {
        var personId = caller.PersonId();
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new UpdateHydrationConfigCommand(personId, dailyWaterTargetMl, glassSizeMl, trackWaterIntake), ct);
            return true;
        });
        return await queries.SendAsync<GetHydrationConfigQuery, HydrationConfigDto?>(new GetHydrationConfigQuery(personId), ct)
            ?? throw new McpException("The settings were saved but could not be read back.");
    }

    [McpServerTool(Name = "log_water_intake", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Adds one drink to the caller's log for a day, in millilitres (a glass is the configured glass size from get_hydration). Each call adds another drink: retry with the same operationKey and fields to get the original entry back instead of counting it twice; a new drink needs a new key.")]
    public static async Task<LoggedWaterIntakeDto> LogWaterIntake(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Idempotency key for this drink, 8-128 characters (a UUID works). Reuse it only to retry this same call.")] string operationKey,
        [Description("Calendar day the drink counts towards, ISO yyyy-MM-dd.")] DateOnly date,
        [Description("Volume in millilitres, 1-5000.")] int amountMl,
        [Description("Short note, if any.")] string? note = null,
        CancellationToken ct = default)
    {
        Guid id = await McpErrors.AsToolErrorsAsync(() => commands.SendAsync<LogWaterIntakeCommand, Guid>(
            new LogWaterIntakeCommand(caller.PersonId(), date, amountMl, note, operationKey), ct));
        return new LoggedWaterIntakeDto(id, date, amountMl);
    }

    [McpServerTool(Name = "delete_water_intake", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Removes one drink from the caller's log, lowering that day's total. There is no undo: confirm with the user first. Repeating the call after a successful delete reports not found; the drink is already gone.")]
    public static async Task<DeletedItemDto> DeleteWaterIntake(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Drink entry id from get_hydration.")] Guid intakeId,
        CancellationToken ct = default)
    {
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new DeleteWaterIntakeCommand(intakeId, caller.PersonId()), ct);
            return true;
        });
        return new DeletedItemDto(intakeId);
    }
}
