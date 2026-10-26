namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.DeleteWeightEntry;
using DietPlanner.Application.Commands.LogWeightEntry;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Writes to the caller's weight log, through the same commands as REST. A day holds one weigh-in,
/// so logging the same day again corrects it instead of adding a second entry.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class WeightTools
{
    [McpServerTool(Name = "log_weight", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Records the caller's weight in kilograms for a day, not in the future. A day holds one weigh-in: logging a day that already has one replaces it, so a retry never adds a second entry. Also sets the profile's current weight, unless a later weigh-in exists, so the caller needs a profile first (set_profile). Returns the entry id and whether it was new.")]
    public static async Task<LogWeightEntryResult> LogWeight(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Calendar day of the weigh-in, ISO yyyy-MM-dd; today or earlier.")] DateOnly date,
        [Description("Weight in kilograms (convert from lb or st first); more than 0, at most 999.")] decimal weightKg,
        CancellationToken ct = default)
        => await McpErrors.AsToolErrorsAsync(() => commands.SendAsync<LogWeightEntryCommand, LogWeightEntryResult>(
            new LogWeightEntryCommand(caller.PersonId(), date, weightKg), ct));

    [McpServerTool(Name = "delete_weight_entry", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Deletes one weigh-in from the caller's weight log; the profile's current weight falls back to the latest remaining entry, or becomes unknown. There is no undo: confirm with the user first. Repeating the call after a successful delete reports not found; the entry is already gone.")]
    public static async Task<DeletedItemDto> DeleteWeightEntry(
        McpCaller caller,
        ICommandDispatcher commands,
        [Description("Weight entry id from get_weight_entries.")] Guid entryId,
        CancellationToken ct = default)
    {
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new DeleteWeightEntryCommand(caller.PersonId(), entryId), ct);
            return true;
        });
        return new DeletedItemDto(entryId);
    }
}
