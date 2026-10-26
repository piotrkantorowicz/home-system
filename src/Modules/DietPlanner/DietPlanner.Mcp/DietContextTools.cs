namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Queries.GetGoal;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Application.Queries.GetProfile;
using DietPlanner.Application.Queries.GetWeightPrediction;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Read-only planning context for the caller: profile, goal, meal slots and the weight prediction.
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

    [McpServerTool(Name = "get_weight_prediction", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Estimates from the caller's profile what eating a given number of kcal a day would do: BMR, TDEE, daily deficit (negative is a surplus), weekly weight change in kg, the date the target weight would be reached, and BMI now and at target. The same calculation as the app; an estimate, not medical advice. Needs date of birth, gender, height, current weight and activity level in the profile.")]
    public static async Task<WeightPredictionDto> GetWeightPrediction(
        McpCaller caller,
        IQueryDispatcher dispatcher,
        [Description("Planned daily intake in kcal, e.g. the goal's daily calorie target.")] decimal dailyCalorieTarget,
        CancellationToken ct = default)
        => await McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetWeightPredictionQuery, WeightPredictionDto?>(
                new GetWeightPredictionQuery(caller.PersonId(), dailyCalorieTarget), ct))
            ?? throw new McpException("The profile is missing date of birth, gender, height, current weight or activity level; set them with set_profile first.");
}
