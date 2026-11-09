namespace Assistant.IntegrationTests.Api;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using Assistant.Api;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

/// <summary>#807 — GenAI spans and metrics, tool-call spans and counters, turn counter, and no user content in any tag.</summary>
public sealed class AssistantTelemetryTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>
{
    private const string Secret = "my-secret-prompt-7f3a";
    private const string Reply = "A reply nobody may export";

    /// <summary>A turn records a chat span and token usage with the model, and counts as completed.</summary>
    [Fact]
    public async Task Turn_WithFakeModel_RecordsChatSpanTokenUsageAndCompletedTurn()
    {
        using var factory = new AssistantApiFactory(databases);
        string model = $"scripted-{Guid.CreateVersion7():N}"; // unique: other test hosts report "scripted-model" too
        factory.Model.ModelId = model;
        using var spans = new SpanProbe();
        using var turns = Collect<long>(factory, "homesystem.assistant.turns");
        using var tokenUsage = new TokenUsageListener();
        var me = spans.Trace(await factory.HouseholdOwnerAsync());
        factory.Model.Answers(Reply);

        await AssistantApiFactory.SendAsync(me, new { message = Secret });

        spans.Spans.ShouldContain(a => a.GetTagItem("gen_ai.operation.name") as string == "chat" && a.GetTagItem("gen_ai.request.model") as string == model);
        tokenUsage.Models.ShouldContain(model);
        turns.GetMeasurementSnapshot().Select(m => m.Tags["outcome"]).ShouldBe(["completed"]);
        AssertNoContent(spans, Reply, Secret);
    }

    /// <summary>A read tool gets a child span and a counter tick; a tool that is not offered records outcome refused under the name "unknown".</summary>
    [Fact]
    public async Task ToolCalls_RecordSpanAndCounter_IncludingRefused()
    {
        using var factory = new AssistantApiFactory(databases);
        using var spans = new SpanProbe();
        using var calls = Collect<long>(factory, "homesystem.assistant.tool_calls");
        var me = spans.Trace(await factory.HouseholdOwnerAsync());
        factory.Model.CallsTools(("list_wallets", []), (Secret, new() { ["secret"] = Secret }));

        await AssistantApiFactory.SendAsync(me, new { message = "Wallets?" });

        Activity ran = spans.Spans.Single(a => a.GetTagItem("gen_ai.tool.name") as string == "list_wallets");
        ran.GetTagItem("gen_ai.operation.name").ShouldBe("execute_tool");
        ran.GetTagItem("homesystem.assistant.outcome").ShouldBe("ok");
        ran.Parent.ShouldNotBeNull();
        spans.Spans.Single(a => a.GetTagItem("gen_ai.tool.name") as string == "unknown").GetTagItem("homesystem.assistant.outcome").ShouldBe("refused");
        var counted = calls.GetMeasurementSnapshot().Select(m => (Tool: (string?)m.Tags["tool"], Outcome: (string?)m.Tags["outcome"])).ToList();
        counted.ShouldBe([("list_wallets", "ok"), ("unknown", "refused")], ignoreOrder: true);
        AssertNoContent(spans, Secret);
    }

    /// <summary>A provider failure counts as failed; hitting the tool-round cap counts as limit_reached.</summary>
    [Fact]
    public async Task Turn_ProviderFailureAndRoundCap_RecordFailedAndLimitReached()
    {
        using var failing = new AssistantApiFactory(databases);
        using var failedTurns = Collect<long>(failing, "homesystem.assistant.turns");
        using var failSpans = new SpanProbe();
        failing.Model.Fails();
        await AssistantApiFactory.SendAsync(failSpans.Trace(await failing.HouseholdOwnerAsync()), new { message = Secret });
        Activity chat = failSpans.Spans.Single(a => a.GetTagItem("gen_ai.operation.name") as string == "chat");
        chat.Status.ShouldBe(ActivityStatusCode.Error);
        AssertNoContent(failSpans, Secret);

        using var capped = new AssistantApiFactory(databases, new Dictionary<string, string?> { ["Assistant:MaxToolRounds"] = "1" });
        using var cappedTurns = Collect<long>(capped, "homesystem.assistant.turns");
        capped.Model.CallsTool("list_wallets");
        await AssistantApiFactory.SendAsync(await capped.HouseholdOwnerAsync(), new { message = "Hi" });

        failedTurns.GetMeasurementSnapshot().Select(m => m.Tags["outcome"]).ShouldBe(["failed"]);
        cappedTurns.GetMeasurementSnapshot().Select(m => m.Tags["outcome"]).ShouldBe(["limit_reached"]);
    }

    /// <summary>A throw after the turn's last model call (while the history is built) counts the turn once, as failed.</summary>
    [Fact]
    public async Task Turn_ThrowingAfterTheLastContent_RecordsOnlyFailed()
    {
        using var factory = new AssistantApiFactory(databases);
        using var turns = Collect<long>(factory, "homesystem.assistant.turns");
        factory.Model.AnswersThenBreaksHistory(Reply);

        List<System.Net.ServerSentEvents.SseItem<string>> events =
            await AssistantApiFactory.SendAsync(await factory.HouseholdOwnerAsync(), new { message = Secret });

        events.Select(e => e.EventType).ShouldContain("error");
        turns.GetMeasurementSnapshot().Select(m => m.Tags["outcome"]).ShouldBe(["failed"]);
    }

    /// <summary>A tool call cancelled by the client ends its span without an outcome and counts nothing, like a cancelled turn.</summary>
    [Fact]
    public async Task ToolCall_CancelledByClient_RecordsNoOutcomeAndLeavesStatusUnset()
    {
        var hanging = new HangingTool();
        using var factory = new AssistantApiFactory(databases);
        factory.ExtraTools.Add(hanging.Create());
        using var spans = new SpanProbe();
        using var calls = Collect<long>(factory, "homesystem.assistant.tool_calls");
        using var turns = Collect<long>(factory, "homesystem.assistant.turns");
        var me = spans.Trace(await factory.HouseholdOwnerAsync());
        factory.Model.CallsTool("hang_tool");
        using var cts = new CancellationTokenSource();

        Task<HttpResponseMessage> request = me.PostAsJsonAsync("/api/assistant/messages", new { message = "Hang" }, cts.Token);
        await hanging.Started.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(request);

        // The server span stops last, after the turn handler has seen the cancellation; asserting earlier could pass vacuously.
        SpinWait.SpinUntil(() => spans.Spans.Any(a => a.Kind == ActivityKind.Server), TimeSpan.FromSeconds(30)).ShouldBeTrue();
        Activity span = spans.Spans.Single(a => a.GetTagItem("gen_ai.tool.name") as string == "hang_tool");
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("homesystem.assistant.outcome").ShouldBeNull();
        calls.GetMeasurementSnapshot().ShouldBeEmpty();
        turns.GetMeasurementSnapshot().ShouldBeEmpty();
    }

    /// <summary>A failure is counted even after the client cancelled; every other outcome is dropped once cancelled.</summary>
    /// <param name="outcome">The turn outcome.</param>
    /// <param name="cancelled">Whether the request was cancelled.</param>
    /// <param name="counted">Whether the turn is counted.</param>
    [Theory]
    [InlineData("failed", true, true)]
    [InlineData("failed", false, true)]
    [InlineData("completed", true, false)]
    [InlineData("limit_reached", true, false)]
    [InlineData("completed", false, true)]
    [InlineData("limit_reached", false, true)]
    public void CountsTurn_FailureAlways_OthersOnlyWhenNotCancelled(string outcome, bool cancelled, bool counted)
        => AssistantTelemetry.CountsTurn(outcome, cancelled).ShouldBe(counted);

    private static MetricCollector<T> Collect<T>(AssistantApiFactory factory, string instrument)
        where T : struct
        => new(factory.Services.GetRequiredService<IMeterFactory>(), "HomeSystem.Assistant", instrument);

    private static void AssertNoContent(SpanProbe probe, params string[] texts)
    {
        foreach (Activity span in probe.Spans)
            foreach (string text in texts)
                span.TagObjects.ShouldAllBe(t => !(t.Value == null ? "" : t.Value.ToString()!).Contains(text, StringComparison.Ordinal));
    }
}
