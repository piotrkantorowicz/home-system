namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Queries.GetWeightEntries;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's weight log.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.ReadScope)]
internal sealed class WeightLogTools
{
    [McpServerTool(Name = "get_weight_entries", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Lists the caller's weigh-ins (kilograms, one per day) oldest first, optionally between two dates (inclusive). Leave both dates out for the whole log.")]
    public static async Task<IReadOnlyList<WeightEntryDto>> GetWeightEntries(
        McpCaller caller,
        IQueryDispatcher queries,
        [Description("First day, ISO yyyy-MM-dd, if any.")] DateOnly? from = null,
        [Description("Last day, ISO yyyy-MM-dd, if any.")] DateOnly? to = null,
        CancellationToken ct = default)
    {
        if (from > to)
            throw new McpException("from must be on or before to.");
        return await queries.SendAsync<GetWeightEntriesQuery, IReadOnlyList<WeightEntryDto>>(new GetWeightEntriesQuery(caller.PersonId(), from, to), ct);
    }
}
