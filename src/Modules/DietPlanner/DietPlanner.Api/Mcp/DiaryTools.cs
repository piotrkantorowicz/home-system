namespace DietPlanner.Api.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.Application.Queries.GetNutritionSummary;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// The caller's own meal diary over an explicit date range of at most 31 days, through the same
/// queries as REST. There is deliberately no argument to read another person's diary.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.ReadScope)]
internal sealed class DiaryTools
{
    public const int MaxDays = 31;

    [McpServerTool(Name = "get_meal_entries", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Lists the caller's planned and eaten meals between two dates (inclusive, at most 31 days), ordered by date and slot. Use it to find occupied slots before planning.")]
    public static async Task<IReadOnlyList<DiaryMealDto>> GetMealEntries(
        RequestContext<CallToolRequestParams> context,
        IQueryDispatcher dispatcher,
        [Description("First day, ISO yyyy-MM-dd.")] DateOnly from,
        [Description("Last day, ISO yyyy-MM-dd, at most 30 days after from.")] DateOnly to,
        CancellationToken ct = default)
    {
        EnsureRange(from, to);
        IReadOnlyList<MealEntryDto> meals = await dispatcher.SendAsync<GetMealEntriesQuery, IReadOnlyList<MealEntryDto>>(
            new GetMealEntriesQuery(McpCaller.PersonId(context), from, to, McpCaller.AuthSubject(context)), ct);
        return [.. meals.OrderBy(m => m.Date).ThenBy(m => m.MealSlotSortOrder).Select(DiaryMealDto.From)];
    }

    [McpServerTool(Name = "get_nutrition_summary", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Per-day calorie and macro totals of everything planned or eaten by the caller between two dates (inclusive, at most 31 days). Modified meals count what was actually eaten; unknown product values count as zero. Days without meals are omitted.")]
    public static async Task<IReadOnlyList<DailyNutritionDto>> GetNutritionSummary(
        RequestContext<CallToolRequestParams> context,
        IQueryDispatcher dispatcher,
        [Description("First day, ISO yyyy-MM-dd.")] DateOnly from,
        [Description("Last day, ISO yyyy-MM-dd, at most 30 days after from.")] DateOnly to,
        CancellationToken ct = default)
    {
        EnsureRange(from, to);
        return await dispatcher.SendAsync<GetNutritionSummaryQuery, IReadOnlyList<DailyNutritionDto>>(
            new GetNutritionSummaryQuery(McpCaller.PersonId(context), from, to), ct);
    }

    private static void EnsureRange(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new McpException("to must not be before from.");
        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
            throw new McpException($"The range covers {to.DayNumber - from.DayNumber + 1} days; at most {MaxDays} are allowed.");
    }
}
