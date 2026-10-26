namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetDietReminderSettings;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>update_meal_schedule</c> and <c>update_diet_reminders</c>, and the reminder
/// settings in <c>get_diet_context</c>: the same records and constraints as REST, scoped to the caller.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpScheduleToolsTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>The first call creates slots; the next keeps slots sent with their id, adds new ones and drops the rest; REST reads the same.</summary>
    [Fact]
    public async Task UpdateMealSchedule_CreatesThenReplaces_MatchingRest()
    {
        var me = $"sc-schedule-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var created = await McpTestHost.CallAsync<MealScheduleConfigDto>(client, "update_meal_schedule",
            Slots(new { name = "Breakfast", defaultTime = "07:30" }, new { name = "Lunch", defaultTime = "13:00" }));
        var breakfast = created.Slots.Single(s => s.Name == "Breakfast");
        var replaced = await McpTestHost.CallAsync<MealScheduleConfigDto>(client, "update_meal_schedule",
            Slots(new { id = breakfast.Id, name = "Early breakfast", defaultTime = "07:00" }, new { name = "Dinner", defaultTime = "19:00" }));
        var rest = await _mcp.Rest(me).GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule", TestContext.Current.CancellationToken);

        replaced.Slots.Select(s => s.Name).ShouldBe(["Early breakfast", "Dinner"]);
        replaced.Slots[0].Id.ShouldBe(breakfast.Id);
        replaced.Slots.Select(s => (s.Id, s.Name, s.DefaultTime)).ShouldBe(rest!.Slots.Select(s => (s.Id, s.Name, s.DefaultTime)));
    }

    /// <summary>Dropping a slot that still has meals, an empty list, too many slots or a bad time are refused and change nothing.</summary>
    [Fact]
    public async Task UpdateMealSchedule_KeepsExistingConstraints()
    {
        var me = $"sc-constraints-{_tag}";
        var recipe = $"Stew {_tag}";
        var import = new ImportDto(
            [new ImportProductDto($"{recipe} base", 120m, 4m, 20m, 2m, 3m, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto($"{recipe} base", 250m, "g")])],
            [new ImportScheduleDto("2026-11-05", [new ImportMealDto("Lunch", recipe, 1m, null)])]);
        (await _mcp.Rest(me).PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        var before = (await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", [])).MealSlots;

        var inUse = await McpTestHost.CallExpectingErrorAsync(client, "update_meal_schedule", Slots(new { name = "Brunch", defaultTime = "11:00" }));
        var empty = await McpTestHost.CallExpectingErrorAsync(client, "update_meal_schedule", Slots());
        var many = await McpTestHost.CallExpectingErrorAsync(client, "update_meal_schedule",
            Slots([.. Enumerable.Range(1, 9).Select(i => (object)new { name = $"Slot {i}", defaultTime = "12:00" })]));
        var time = await McpTestHost.CallExpectingErrorAsync(client, "update_meal_schedule", Slots(new { name = "Snack", defaultTime = "noon" }));
        var after = (await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", [])).MealSlots;

        inUse.ShouldContain("has logged entries");
        empty.ShouldContain("At least 1 meal slot");
        many.ShouldContain("more than 8");
        time.ShouldContain("DefaultTime");
        after.ShouldBe(before);
    }

    /// <summary>Reminder settings are created then replaced, match REST, and show up in get_diet_context.</summary>
    [Fact]
    public async Task UpdateDietReminders_CreatesThenReplaces_AndShowsInContext()
    {
        var me = $"sc-reminders-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        var none = await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", []);

        await McpTestHost.CallAsync<DietReminderSettingsDto>(client, "update_diet_reminders", Reminders(lead: 30));
        var replaced = await McpTestHost.CallAsync<DietReminderSettingsDto>(client, "update_diet_reminders", Reminders(lead: 15, day: "Monday"));
        var rest = await _mcp.Rest(me).GetFromJsonAsync<DietReminderSettingsDto>("/api/v1/diet-reminder-settings", TestContext.Current.CancellationToken);
        var context = await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", []);

        none.Reminders.ShouldBeNull();
        replaced.ShouldBe(rest);
        replaced.MealReminderLeadTimeMinutes.ShouldBe(15);
        replaced.WeeklySummaryDayOfWeek.ShouldBe(DayOfWeek.Monday);
        context.Reminders.ShouldBe(rest);
    }

    /// <summary>Out-of-range values and a water window that ends before it starts are field errors and save nothing.</summary>
    [Fact]
    public async Task UpdateDietReminders_RejectsInvalidValues()
    {
        var me = $"sc-rem-invalid-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var lead = await McpTestHost.CallExpectingErrorAsync(client, "update_diet_reminders", Reminders(lead: 500));
        var window = await McpTestHost.CallExpectingErrorAsync(client, "update_diet_reminders", Reminders(windowStart: "20:00", windowEnd: "08:00"));

        lead.ShouldContain("MealReminderLeadTimeMinutes");
        window.ShouldContain("WaterWindowEnd");
        (await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", [])).Reminders.ShouldBeNull();
    }

    /// <summary>Each caller only sees and changes their own schedule and reminders.</summary>
    [Fact]
    public async Task ScheduleTools_AreScopedToTheCaller()
    {
        var (me, other) = ($"sc-me-{_tag}", $"sc-other-{_tag}");
        await using var mine = await _mcp.ConnectAsync(me, WriteScopes);
        await using var theirs = await _mcp.ConnectAsync(other, WriteScopes);
        await McpTestHost.CallAsync<MealScheduleConfigDto>(mine, "update_meal_schedule", Slots(new { name = "Lunch", defaultTime = "13:00" }));
        await McpTestHost.CallAsync<DietReminderSettingsDto>(mine, "update_diet_reminders", Reminders(lead: 30));

        var view = await McpTestHost.CallAsync<DietContextDto>(theirs, "get_diet_context", []);
        await McpTestHost.CallAsync<MealScheduleConfigDto>(theirs, "update_meal_schedule", Slots(new { name = "Dinner", defaultTime = "19:00" }));
        await McpTestHost.CallAsync<DietReminderSettingsDto>(theirs, "update_diet_reminders", Reminders(lead: 60));
        var after = await McpTestHost.CallAsync<DietContextDto>(mine, "get_diet_context", []);

        view.MealSlots.ShouldBeEmpty();
        view.Reminders.ShouldBeNull();
        after.MealSlots.ShouldHaveSingleItem().Name.ShouldBe("Lunch");
        after.Reminders!.MealReminderLeadTimeMinutes.ShouldBe(30);
    }

    /// <summary>A read-only token neither lists nor runs the schedule and reminder writes.</summary>
    [Theory]
    [InlineData("update_meal_schedule")]
    [InlineData("update_diet_reminders")]
    public async Task ScheduleWrites_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"sc-ro-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var args = Reminders(lead: 30);
        args["slots"] = new[] { new { name = "Lunch", defaultTime = "13:00" } };

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, args, cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        var context = await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", []);
        context.MealSlots.ShouldBeEmpty();
        context.Reminders.ShouldBeNull();
    }

    /// <summary>The assistant can run the schedule tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task ScheduleTools_InProcess()
    {
        var me = $"sc-inproc-{_tag}";
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var schedule = await invoker.InvokeAsync(user, "update_meal_schedule", Json(Slots(new { name = "Lunch", defaultTime = "13:00" })), TestContext.Current.CancellationToken);
        var reminders = await invoker.InvokeAsync(user, "update_diet_reminders", Json(Reminders(lead: 20)), TestContext.Current.CancellationToken);

        new[] { schedule, reminders }.ShouldAllBe(r => r.IsError != true);
        (await _mcp.Rest(me).GetFromJsonAsync<DietReminderSettingsDto>("/api/v1/diet-reminder-settings", TestContext.Current.CancellationToken))!
            .MealReminderLeadTimeMinutes.ShouldBe(20);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> Slots(params object[] slots) => new() { ["slots"] = slots };

    private static Dictionary<string, object?> Reminders(int lead = 30, string day = "Sunday", string windowStart = "08:00", string windowEnd = "21:00")
        => new()
        {
            ["mealRemindersEnabled"] = true,
            ["mealReminderLeadTimeMinutes"] = lead,
            ["mealMissedGraceMinutes"] = 60,
            ["waterRemindersEnabled"] = true,
            ["waterReminderIntervalMinutes"] = 90,
            ["waterWindowStart"] = windowStart,
            ["waterWindowEnd"] = windowEnd,
            ["weeklySummaryEnabled"] = true,
            ["weeklySummaryDayOfWeek"] = day,
            ["weeklySummaryTimeOfDay"] = "18:00",
            ["goalAlertsEnabled"] = false,
        };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));
}
