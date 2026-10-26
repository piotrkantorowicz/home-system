namespace DietPlanner.Mcp;

using System.ComponentModel;
using DietPlanner.Application.Commands.UpdateDietReminderSettings;
using DietPlanner.Application.Commands.UpdateMealSchedule;
using DietPlanner.Application.Queries.GetDietReminderSettings;
using DietPlanner.Application.Queries.GetMealSchedule;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// The caller's own meal schedule and reminder settings, through the same commands as REST PUT.
/// Both replace the whole record, creating it on first use.
/// </summary>
[McpServerToolType]
[Authorize(Policy = DietPlannerMcp.WriteScope)]
internal sealed class ScheduleTools
{
    [McpServerTool(Name = "update_meal_schedule", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Replaces the caller's meal slots (1-8, in display order), creating the schedule on first use. Send every slot that should stay with its id from get_diet_context; a slot without an id is new, and a slot left out is removed. Removing a slot that still has meals in the diary is refused; delete or move those meals first.")]
    public static async Task<MealScheduleConfigDto> UpdateMealSchedule(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("The complete list of slots, in display order.")] IReadOnlyList<MealSlotEdit> slots,
        CancellationToken ct = default)
    {
        var (personId, subject) = (caller.PersonId(), caller.AuthSubject());
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new UpdateMealScheduleCommand(personId,
                [.. slots.Select(s => new MealSlotInput(s.Id, s.Name, s.DefaultTime))], subject), ct);
            return true;
        });
        return await queries.SendAsync<GetMealScheduleQuery, MealScheduleConfigDto?>(new GetMealScheduleQuery(personId, subject), ct)
            ?? throw new McpException("The schedule was saved but could not be read back.");
    }

    [McpServerTool(Name = "update_diet_reminders", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Replaces all of the caller's reminder settings, creating them on first use: meal reminders, water reminders, the weekly summary and goal alerts. Every value is required, so read get_diet_context first and send back what should stay. Times are local wall-clock HH:mm in the caller's configured time zone.")]
    public static async Task<DietReminderSettingsDto> UpdateDietReminders(
        McpCaller caller,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        [Description("Remind before planned meals.")] bool mealRemindersEnabled,
        [Description("Minutes before a meal to remind, 1-120.")] int mealReminderLeadTimeMinutes,
        [Description("Minutes after a meal's time before it counts as missed, 1-240.")] int mealMissedGraceMinutes,
        [Description("Remind to drink water.")] bool waterRemindersEnabled,
        [Description("Minutes between water reminders, 15-480.")] int waterReminderIntervalMinutes,
        [Description("Start of the water reminder window, HH:mm.")] TimeOnly waterWindowStart,
        [Description("End of the water reminder window, HH:mm, after the start.")] TimeOnly waterWindowEnd,
        [Description("Send a weekly summary.")] bool weeklySummaryEnabled,
        [Description("Day of the weekly summary, e.g. Sunday.")] DayOfWeek weeklySummaryDayOfWeek,
        [Description("Time of the weekly summary, HH:mm.")] TimeOnly weeklySummaryTimeOfDay,
        [Description("Notify on goal milestones.")] bool goalAlertsEnabled,
        CancellationToken ct = default)
    {
        var personId = caller.PersonId();
        await McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new UpdateDietReminderSettingsCommand(personId, mealRemindersEnabled, mealReminderLeadTimeMinutes,
                mealMissedGraceMinutes, waterRemindersEnabled, waterReminderIntervalMinutes, waterWindowStart, waterWindowEnd,
                weeklySummaryEnabled, weeklySummaryDayOfWeek, weeklySummaryTimeOfDay, goalAlertsEnabled), ct);
            return true;
        });
        return await queries.SendAsync<GetDietReminderSettingsQuery, DietReminderSettingsDto?>(new GetDietReminderSettingsQuery(personId), ct)
            ?? throw new McpException("The reminder settings were saved but could not be read back.");
    }
}
