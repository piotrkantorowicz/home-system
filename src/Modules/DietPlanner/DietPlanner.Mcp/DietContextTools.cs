namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Queries.GetGoal;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Application.Queries.GetProfile;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Read-only planning context for the caller.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.ReadScope)]
internal sealed class DietContextTools
{
    [McpServerTool(Name = "get_diet_context", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the caller's diet profile, daily goal and meal slots, plus a list of setup still missing in the app. Call it before planning meals; it never creates or changes anything.")]
    public static async Task<DietContextDto> GetDietContext(
        McpCaller caller,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = caller.PersonId();

        UserProfileDto? profile = await dispatcher.SendAsync<GetProfileQuery, UserProfileDto?>(new GetProfileQuery(personId), ct);
        GoalDto? goal = await dispatcher.SendAsync<GetGoalQuery, GoalDto?>(new GetGoalQuery(personId), ct);
        MealScheduleConfigDto? schedule = await dispatcher.SendAsync<GetMealScheduleQuery, MealScheduleConfigDto?>(
            new GetMealScheduleQuery(personId, caller.AuthSubject()), ct);

        var slots = schedule?.Slots.OrderBy(s => s.SortOrder).ToList() ?? [];
        var missing = new List<string>();
        if (profile is null) missing.Add("profile");
        if (goal is null) missing.Add("goal");
        if (slots.Count == 0) missing.Add("meal_schedule");

        return new DietContextDto(profile, goal, slots, missing);
    }
}
