namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Queries.GetHydrationConfig;
using DietPlanner.Application.Queries.GetWaterIntake;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's hydration settings and logged drinks.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.ReadScope)]
internal sealed class HydrationReadTools
{
    [McpServerTool(Name = "get_hydration", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the caller's hydration settings (daily water target, glass size, whether tracking is on; null until set) and the drinks logged on one day with their total, all in millilitres.")]
    public static async Task<HydrationDto> GetHydration(
        McpCaller caller,
        IQueryDispatcher queries,
        [Description("Calendar day, ISO yyyy-MM-dd.")] DateOnly date,
        CancellationToken ct = default)
    {
        var personId = caller.PersonId();
        HydrationConfigDto? config = await queries.SendAsync<GetHydrationConfigQuery, HydrationConfigDto?>(new GetHydrationConfigQuery(personId), ct);
        WaterIntakeListDto intake = await queries.SendAsync<GetWaterIntakeQuery, WaterIntakeListDto>(new GetWaterIntakeQuery(personId, date), ct);
        return new HydrationDto(config, intake);
    }
}
