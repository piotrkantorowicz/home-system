namespace Assistant.IntegrationTests.Api;

using System.Diagnostics;
using System.Diagnostics.Metrics;
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
        using var spans = new SpanProbe();
        using var turns = Collect<long>(factory, "homesystem.assistant.turns");
        using var tokenUsage = new TokenUsageListener();
        var me = spans.Trace(await factory.HouseholdOwnerAsync());
        factory.Model.Answers(Reply);

        await AssistantApiFactory.SendAsync(me, new { message = Secret });

        spans.Spans.ShouldContain(a => a.GetTagItem("gen_ai.operation.name") as string == "chat" && a.GetTagItem("gen_ai.request.model") as string == "scripted-model");
        tokenUsage.Models.ShouldContain("scripted-model");
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
        await AssistantApiFactory.SendAsync(failSpans.Trace(await failing.HouseholdOwnerAsync()), new { message = "Hi" });

        using var capped = new AssistantApiFactory(databases, new Dictionary<string, string?> { ["Assistant:MaxToolRounds"] = "1" });
        using var cappedTurns = Collect<long>(capped, "homesystem.assistant.turns");
        capped.Model.CallsTool("list_wallets");
        await AssistantApiFactory.SendAsync(await capped.HouseholdOwnerAsync(), new { message = "Hi" });

        failedTurns.GetMeasurementSnapshot().Select(m => m.Tags["outcome"]).ShouldBe(["failed"]);
        cappedTurns.GetMeasurementSnapshot().Select(m => m.Tags["outcome"]).ShouldBe(["limit_reached"]);
    }

    /// <summary>A call the client cancelled ends its span without an outcome and counts nothing, like a cancelled turn.</summary>
    [Fact]
    public void AbandonToolCall_RecordsNoOutcomeAndLeavesStatusUnset()
    {
        using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        _ = new AssistantTelemetry(services.GetRequiredService<IMeterFactory>());
        using var calls = new MetricCollector<long>(services.GetRequiredService<IMeterFactory>(), "HomeSystem.Assistant", "homesystem.assistant.tool_calls");
        using var spans = new SpanProbe();

        Activity? span = AssistantTelemetry.StartToolCall("list_wallets");
        AssistantTelemetry.AbandonToolCall(span);

        calls.GetMeasurementSnapshot().ShouldBeEmpty();
        span.ShouldNotBeNull();
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("homesystem.assistant.outcome").ShouldBeNull();
    }

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
