namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.CreateGoal;
using DietPlanner.Application.Commands.CreateProfile;
using DietPlanner.Application.Commands.UpdateGoal;
using DietPlanner.Application.Commands.UpdateProfile;
using DietPlanner.Application.Queries.GetGoal;
using DietPlanner.Application.Queries.GetProfile;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// The caller's own diet profile and daily goal. Each tool creates the record on first use and
/// replaces it afterwards, through the same create/update commands as REST POST/PUT.
/// ponytail: read-then-write upsert; two first calls racing hit the unique person index and one fails, a retry succeeds.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class DietSetupTools
{
    [McpServerTool(Name = "set_profile", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Saves the caller's diet profile, creating it on first use. Replaces every field: values you leave out become unknown, so read get_diet_context first and send back what should stay. Height in cm, weights in kg.")]
    public static async Task<UserProfileDto> SetProfile(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Date of birth, ISO yyyy-MM-dd, if known.")] DateOnly? dateOfBirth = null,
        [Description("Male, Female or Other, if known.")] string? gender = null,
        [Description("Height in cm, if known.")] decimal? heightCm = null,
        [Description("Current weight in kg, if known.")] decimal? currentWeightKg = null,
        [Description("Target weight in kg, if any.")] decimal? targetWeightKg = null,
        [Description("Sedentary, LightlyActive, ModeratelyActive, VeryActive or ExtraActive, if known.")] string? activityLevel = null,
        CancellationToken ct = default)
    {
        var personId = caller.PersonId();
        var exists = await queries.SendAsync<GetProfileQuery, UserProfileDto?>(new GetProfileQuery(personId), ct) is not null;
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            if (exists)
                await commands.SendAsync(new UpdateProfileCommand(personId, dateOfBirth, gender, heightCm, currentWeightKg, targetWeightKg, activityLevel), ct);
            else
                await commands.SendAsync<CreateProfileCommand, Guid>(new CreateProfileCommand(personId, dateOfBirth, gender, heightCm, currentWeightKg, targetWeightKg, activityLevel), ct);
            return true;
        });
        return await queries.SendAsync<GetProfileQuery, UserProfileDto?>(new GetProfileQuery(personId), ct)
            ?? throw new McpException("The profile was saved but could not be read back.");
    }

    [McpServerTool(Name = "set_goals", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Saves the caller's daily nutrition goal, creating it on first use. Replaces every field: values you leave out become unset, so read get_diet_context first and send back what should stay. get_weight_prediction shows what a calorie target would do to weight.")]
    public static async Task<GoalDto> SetGoals(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Daily energy target in kcal, if any.")] int? dailyCalorieTarget = null,
        [Description("Daily protein in grams, if any.")] decimal? proteinGrams = null,
        [Description("Daily carbohydrates in grams, if any.")] decimal? carbsGrams = null,
        [Description("Daily fat in grams, if any.")] decimal? fatGrams = null,
        [Description("Daily fibre in grams, if any.")] decimal? fiberGrams = null,
        CancellationToken ct = default)
    {
        var personId = caller.PersonId();
        var exists = await queries.SendAsync<GetGoalQuery, GoalDto?>(new GetGoalQuery(personId), ct) is not null;
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            if (exists)
                await commands.SendAsync(new UpdateGoalCommand(personId, dailyCalorieTarget, proteinGrams, carbsGrams, fatGrams, fiberGrams), ct);
            else
                await commands.SendAsync<CreateGoalCommand, Guid>(new CreateGoalCommand(personId, dailyCalorieTarget, proteinGrams, carbsGrams, fatGrams, fiberGrams), ct);
            return true;
        });
        return await queries.SendAsync<GetGoalQuery, GoalDto?>(new GetGoalQuery(personId), ct)
            ?? throw new McpException("The goal was saved but could not be read back.");
    }
}
