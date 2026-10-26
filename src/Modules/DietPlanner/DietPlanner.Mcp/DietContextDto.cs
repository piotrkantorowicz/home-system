namespace DietPlanner.Mcp;

using DietPlanner.Application.Queries.GetDietReminderSettings;
using DietPlanner.Application.Queries.GetGoal;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Application.Queries.GetProfile;

/// <summary>
/// What an agent needs before planning meals for the caller.
/// </summary>
/// <param name="Profile">Body data and activity level, or <see langword="null"/> when not set up.</param>
/// <param name="Goal">Daily calorie and macro targets, or <see langword="null"/> when not set up.</param>
/// <param name="MealSlots">The caller's meal slots in display order; imported meals must name one of them.</param>
/// <param name="MissingSetup">Setup still missing (<c>profile</c>, <c>goal</c>, <c>meal_schedule</c>); this tool never creates it, <c>set_profile</c>, <c>set_goals</c> and <c>update_meal_schedule</c> do.</param>
/// <param name="Reminders">Meal, water, weekly-summary and goal-alert reminder settings, or <see langword="null"/> when never set; times are local wall-clock.</param>
public sealed record DietContextDto(
    UserProfileDto? Profile,
    GoalDto? Goal,
    IReadOnlyList<MealSlotDto> MealSlots,
    IReadOnlyList<string> MissingSetup,
    DietReminderSettingsDto? Reminders);
