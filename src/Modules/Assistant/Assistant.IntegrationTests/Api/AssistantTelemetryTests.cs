namespace Assistant.IntegrationTests.Api;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>#807 — GenAI spans and metrics, tool-call spans and counters, turn counter, and no user content in any tag.</summary>
public sealed class AssistantTelemetryTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>
{
    private const string Secret = "my-secret-prompt-7f3a";

    /// <summary>A turn records a chat span and token usage with the model, and counts as completed.</summary>
    [Fact]
    public async Task Turn_WithFakeModel_RecordsChatSpanTokenUsageAndCompletedTurn()
    {
        using var factory = new AssistantApiFactory(databases);
        using var probe = new TelemetryProbe(factory.Services.GetRequiredService<IMeterFactory>());
        var me = await factory.HouseholdOwnerAsync();
        factory.Model.Answers("A reply nobody may export");

        await AssistantApiFactory.SendAsync(me, new { message = Secret });

        probe.Spans.ShouldContain(a => a.GetTagItem("gen_ai.operation.name") as string == "chat" && a.GetTagItem("gen_ai.request.model") as string == "scripted-model");
        probe.Of("gen_ai.client.token.usage").ShouldContain(t => t.GetValueOrDefault("gen_ai.request.model") as string == "scripted-model");
        probe.Of("homesystem.assistant.turns").ShouldContain(t => t["outcome"] as string == "completed");
        AssertNoContent(probe, "A reply nobody may export");
    }

    /// <summary>A read tool gets a child span and a counter tick; a tool that is not offered records outcome refused.</summary>
    [Fact]
    public async Task ToolCalls_RecordSpanAndCounter_IncludingRefused()
    {
        using var factory = new AssistantApiFactory(databases);
        using var probe = new TelemetryProbe(factory.Services.GetRequiredService<IMeterFactory>());
        var me = await factory.HouseholdOwnerAsync();
        factory.Model.CallsTools(("list_wallets", []), ("not_a_tool", new() { ["secret"] = Secret }));

        await AssistantApiFactory.SendAsync(me, new { message = "Wallets?" });

        Activity ran = probe.Spans.Single(a => a.GetTagItem("gen_ai.tool.name") as string == "list_wallets");
        ran.GetTagItem("gen_ai.operation.name").ShouldBe("execute_tool");
        ran.GetTagItem("homesystem.assistant.outcome").ShouldBe("ok");
        ran.Parent.ShouldNotBeNull();
        probe.Spans.Single(a => a.GetTagItem("gen_ai.tool.name") as string == "not_a_tool").GetTagItem("homesystem.assistant.outcome").ShouldBe("refused");
        var calls = probe.Of("homesystem.assistant.tool_calls");
        calls.ShouldContain(t => t["tool"] as string == "list_wallets" && t["outcome"] as string == "ok");
        calls.ShouldContain(t => t["tool"] as string == "not_a_tool" && t["outcome"] as string == "refused");
        AssertNoContent(probe, Secret);
    }

    /// <summary>A provider failure counts as failed; hitting the tool-round cap counts as limit_reached.</summary>
    [Fact]
    public async Task Turn_ProviderFailureAndRoundCap_RecordFailedAndLimitReached()
    {
        using var failing = new AssistantApiFactory(databases);
        using var failingProbe = new TelemetryProbe(failing.Services.GetRequiredService<IMeterFactory>());
        failing.Model.Fails();
        await AssistantApiFactory.SendAsync(await failing.HouseholdOwnerAsync(), new { message = "Hi" });

        using var capped = new AssistantApiFactory(databases, new Dictionary<string, string?> { ["Assistant:MaxToolRounds"] = "1" });
        using var cappedProbe = new TelemetryProbe(capped.Services.GetRequiredService<IMeterFactory>());
        capped.Model.CallsTool("list_wallets");
        await AssistantApiFactory.SendAsync(await capped.HouseholdOwnerAsync(), new { message = "Hi" });

        failingProbe.Of("homesystem.assistant.turns").Select(t => t["outcome"]).ShouldBe(["failed"]);
        cappedProbe.Of("homesystem.assistant.turns").Select(t => t["outcome"]).ShouldBe(["limit_reached"]);
    }

    private static void AssertNoContent(TelemetryProbe probe, string text)
    {
        foreach (Activity span in probe.Spans)
            span.TagObjects.ShouldAllBe(t => !(t.Value == null ? "" : t.Value.ToString()!).Contains(text, StringComparison.Ordinal) && !(t.Value == null ? "" : t.Value.ToString()!).Contains(Secret, StringComparison.Ordinal));
        foreach (var (_, tags) in probe.Measurements)
            tags.Values.ShouldAllBe(v => !(v == null ? "" : v.ToString()!).Contains(text, StringComparison.Ordinal));
    }
}
