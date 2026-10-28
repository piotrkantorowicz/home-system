namespace Assistant.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.AI;

/// <summary>#646 — per-user daily token budget, request rate and history allowance. The scripted model reports 110 tokens per call.</summary>
public sealed class AssistantLimitsTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Once today's budget is used, the next request is a 429 and the model is not called; other users are unaffected.</summary>
    [Fact]
    public async Task SendMessage_OverDailyBudget_Returns429WithoutCallingModel()
    {
        using var factory = Factory(("Assistant:DailyTokenBudget", "100"));
        var me = await factory.HouseholdOwnerAsync();
        var other = await factory.HouseholdOwnerAsync();
        await AssistantApiFactory.SendAsync(me, new { message = "First" });

        var refused = await PostAsync(me, "Second");

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await refused.Content.ReadAsStringAsync(Ct)).ShouldContain("allowance");
        factory.Model.Calls.Count.ShouldBe(1);
        (await PostAsync(other, "Mine is fresh")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>The budget is per server day: after midnight the same user is served again.</summary>
    [Fact]
    public async Task SendMessage_NextServerDay_ResetsBudget()
    {
        using var factory = Factory(("Assistant:DailyTokenBudget", "100"));
        var me = await factory.HouseholdOwnerAsync();
        await AssistantApiFactory.SendAsync(me, new { message = "First" });
        (await PostAsync(me, "Second")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        factory.Clock.Advance(TimeSpan.FromHours(3)); // 21:30 → 00:30 next day

        (await PostAsync(me, "Third")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>The budget is re-checked before every model call, so a turn stops mid-way instead of overrunning it.</summary>
    [Fact]
    public async Task SendMessage_BudgetUsedMidTurn_StopsBeforeNextModelCall()
    {
        using var factory = Factory(("Assistant:DailyTokenBudget", "100"));
        var me = await factory.HouseholdOwnerAsync();
        factory.Model.CallsTool("list_wallets");
        factory.Model.Answers("Never sent");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { message = "Wallets?" });

        factory.Model.Calls.Count.ShouldBe(1);
        events.Select(e => e.EventType).ShouldBe(["tool_call", "tool_result", "error", "history", "usage", "done"]);
    }

    /// <summary>More requests than <c>RequestsPerMinute</c> are refused until the next minute.</summary>
    [Fact]
    public async Task SendMessage_OverRequestRate_Returns429UntilNextMinute()
    {
        using var factory = Factory(("Assistant:RequestsPerMinute", "2"));
        var me = await factory.HouseholdOwnerAsync();
        await AssistantApiFactory.SendAsync(me, new { message = "One" });
        await AssistantApiFactory.SendAsync(me, new { message = "Two" });

        var refused = await PostAsync(me, "Three");

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await refused.Content.ReadAsStringAsync(Ct)).ShouldContain("Too many");
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        (await PostAsync(me, "Four")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>The usage event reports the request's tokens and today's running total against the budget.</summary>
    [Fact]
    public async Task SendMessage_SecondRequest_ReportsTodaysTotalAndBudget()
    {
        using var factory = Factory(("Assistant:DailyTokenBudget", "5000"));
        var me = await factory.HouseholdOwnerAsync();
        await AssistantApiFactory.SendAsync(me, new { message = "One" });

        JsonElement usage = Data(await AssistantApiFactory.SendAsync(me, new { message = "Two" }), "usage");

        usage.GetProperty("inputTokens").GetInt64().ShouldBe(100);
        usage.GetProperty("outputTokens").GetInt64().ShouldBe(10);
        usage.GetProperty("todayTokens").GetInt64().ShouldBe(220);
        usage.GetProperty("dailyBudget").GetInt64().ShouldBe(5000);
    }

    /// <summary>History beyond the allowance is dropped oldest-first, whole turns at a time.</summary>
    [Fact]
    public async Task SendMessage_HistoryOverAllowance_SendsOnlyRecentTurns()
    {
        using var factory = Factory(("Assistant:MaxHistoryTokens", "100"));
        var me = await factory.HouseholdOwnerAsync();
        var history = JsonSerializer.SerializeToElement(
            new List<ChatMessage>
            {
                new(ChatRole.User, new string('x', 2000)),
                new(ChatRole.Assistant, "Old answer"),
                new(ChatRole.User, "Recent question"),
                new(ChatRole.Assistant, "Recent answer"),
            },
            AIJsonUtilities.DefaultOptions);

        await AssistantApiFactory.SendAsync(me, new { message = "Now", messages = history });

        factory.Model.Calls.Single().Messages.Where(m => m.Role != ChatRole.System).Select(m => m.Text)
            .ShouldBe(["Recent question", "Recent answer", "Now"]);
    }

    /// <summary>A call that fails after streaming is still charged, so failing every request cannot dodge the daily budget.</summary>
    [Fact]
    public async Task SendMessage_ProviderFailsMidStream_StillCountsTokens()
    {
        using var factory = Factory(("Assistant:DailyTokenBudget", "100"));
        var me = await factory.HouseholdOwnerAsync();
        factory.Model.FailsAfterStreaming("Partial");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { message = "First" });
        var next = await PostAsync(me, "Second");

        events.Select(e => e.EventType).ShouldBe(["text", "error", "done"]);
        next.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        factory.Model.Calls.Count.ShouldBe(1);
    }

    private AssistantApiFactory Factory(params (string Key, string Value)[] settings)
        => new(databases, settings.ToDictionary(s => s.Key, s => (string?)s.Value));

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string message)
        => client.PostAsJsonAsync("/api/assistant/messages", new { message }, Ct);

    private static JsonElement Data(List<SseItem<string>> events, string type)
        => JsonDocument.Parse(events.First(e => e.EventType == type).Data).RootElement;
}
