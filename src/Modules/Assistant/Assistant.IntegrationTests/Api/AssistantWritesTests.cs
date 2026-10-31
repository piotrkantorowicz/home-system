namespace Assistant.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.AI;

/// <summary>#648 — every write is held as a pending action and runs only on the user's Confirm.</summary>
public sealed class AssistantWritesTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>, IDisposable
{
    private readonly AssistantApiFactory _factory = new(databases);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A write the model calls is streamed as a pending action and the turn stops; nothing is written.</summary>
    [Fact]
    public async Task SendMessage_WriteToolCall_IsHeldAndWritesNothing()
    {
        var me = await _factory.HouseholdOwnerAsync();
        _factory.Model.CallsTool("create_wallet", Wallet($"Held {_tag}"));

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { message = "Create a wallet" });

        events.Select(e => e.EventType).ShouldBe(["pending_action", "history", "usage", "done"]);
        JsonElement action = Data(events, "pending_action");
        action.GetProperty("name").GetString().ShouldBe("create_wallet");
        action.GetProperty("arguments").GetProperty("name").GetString().ShouldBe($"Held {_tag}");
        action.GetProperty("destructive").GetBoolean().ShouldBeFalse();
        _factory.Model.Calls.Count.ShouldBe(1);
        _factory.Model.Calls.Single().Options!.Tools!.Select(t => t.Name).ShouldContain("create_wallet");
        (await me.GetStringAsync("/api/investments/wallets", Ct)).ShouldNotContain($"Held {_tag}");
    }

    /// <summary>Confirm runs exactly the held arguments, answers the held call and lets the model continue.</summary>
    [Fact]
    public async Task Confirm_HeldWrite_RunsItAndContinues()
    {
        var me = await _factory.HouseholdOwnerAsync();
        (string token, JsonElement history) = await HoldAsync(me, $"Confirmed {_tag}");
        _factory.Model.Answers("Your wallet is ready.");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { confirm = token, messages = history });

        events.Select(e => e.EventType).ShouldBe(["tool_call", "tool_result", "text", "history", "usage", "done"]);
        Data(events, "tool_result").GetProperty("ok").GetBoolean().ShouldBeTrue();
        (await me.GetStringAsync("/api/investments/wallets", Ct)).ShouldContain($"Confirmed {_tag}");
        FunctionResultContent answer = _factory.Model.Calls.Last().Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Last();
        answer.CallId.ShouldBe(HeldCallId(history));
        answer.Result!.ToString()!.ShouldContain($"Confirmed {_tag}");
    }

    /// <summary>Thinking that came with the held call is left out of stored history but sent back before the call on confirm.</summary>
    [Fact]
    public async Task Confirm_HeldWriteWithReasoning_SendsReasoningBeforeTheCall()
    {
        var me = await _factory.HouseholdOwnerAsync();
        _factory.Model.ThinksThenCallsTool($"thought {_tag}", "create_wallet", Wallet($"Reasoned {_tag}"));
        List<SseItem<string>> held = await AssistantApiFactory.SendAsync(me, new { message = "Create a wallet" });
        JsonElement history = Data(held, "history").GetProperty("messages");
        history.ToString().ShouldNotContain($"thought {_tag}");
        _factory.Model.Answers("Your wallet is ready.");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(
            me, new { confirm = Data(held, "pending_action").GetProperty("token").GetString(), messages = history });

        ChatMessage call = _factory.Model.Calls.Last().Messages.Single(m => m.Contents.OfType<FunctionCallContent>().Any());
        call.Contents[0].ShouldBeOfType<TextReasoningContent>().Text.ShouldBe($"thought {_tag}");
        Data(events, "history").GetProperty("messages").ToString().ShouldNotContain($"thought {_tag}");
    }

    /// <summary>Cancel writes nothing and tells the model the user declined.</summary>
    [Fact]
    public async Task Cancel_HeldWrite_WritesNothingAndTellsModel()
    {
        var me = await _factory.HouseholdOwnerAsync();
        (string token, JsonElement history) = await HoldAsync(me, $"Cancelled {_tag}");
        _factory.Model.Answers("Okay, I left it.");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { cancel = token, messages = history });

        events.Select(e => e.EventType).ShouldBe(["text", "history", "usage", "done"]);
        (await me.GetStringAsync("/api/investments/wallets", Ct)).ShouldNotContain($"Cancelled {_tag}");
        _factory.Model.Calls.Last().Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Last()
            .Result!.ToString()!.ShouldContain("cancelled");
    }

    /// <summary>A token that was altered, belongs to another user or has expired is refused with 400 and runs nothing.</summary>
    /// <param name="case">Which bad token to send.</param>
    [Theory]
    [InlineData("tampered")]
    [InlineData("other-user")]
    [InlineData("expired")]
    public async Task Confirm_BadToken_Returns400AndWritesNothing(string @case)
    {
        var me = await _factory.HouseholdOwnerAsync();
        (string token, JsonElement history) = await HoldAsync(me, $"Bad {_tag}");
        HttpClient caller = me;
        if (@case == "tampered")
            token = string.Concat(token.AsSpan(0, token.Length - 4), token.EndsWith("AAAA", StringComparison.Ordinal) ? "BBBB" : "AAAA");
        else if (@case == "other-user")
            caller = await _factory.HouseholdOwnerAsync();
        else
            _factory.Clock.Advance(TimeSpan.FromMinutes(31));

        var response = await caller.PostAsJsonAsync("/api/assistant/messages", new { confirm = token, messages = history }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _factory.Model.Calls.Count.ShouldBe(1);
        (await me.GetStringAsync("/api/investments/wallets", Ct)).ShouldNotContain($"Bad {_tag}");
    }

    /// <summary>Only one action waits at a time: a second write in the same round is turned back to the model.</summary>
    [Fact]
    public async Task SendMessage_TwoWritesInOneRound_HoldsOnlyTheFirst()
    {
        var me = await _factory.HouseholdOwnerAsync();
        _factory.Model.CallsTools(("create_wallet", Wallet($"First {_tag}")), ("create_wallet", Wallet($"Second {_tag}")));

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { message = "Create two wallets" });

        events.Count(e => e.EventType == "pending_action").ShouldBe(1);
        Data(events, "pending_action").GetProperty("arguments").GetProperty("name").GetString().ShouldBe($"First {_tag}");
        Data(events, "history").GetProperty("messages").ToString().ShouldContain("Only one change can wait");
    }

    /// <summary>Tools annotated destructive are flagged so the card can warn.</summary>
    [Fact]
    public async Task SendMessage_DestructiveWrite_IsFlagged()
    {
        var me = await _factory.HouseholdOwnerAsync();
        _factory.Model.CallsTool("archive_wallet", new() { ["walletId"] = Guid.CreateVersion7() });

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { message = "Archive it" });

        Data(events, "pending_action").GetProperty("destructive").GetBoolean().ShouldBeTrue();
    }

    /// <summary>A request must carry exactly one of message, confirm and cancel.</summary>
    [Fact]
    public async Task SendMessage_MessageAndConfirm_Returns400()
    {
        var me = await _factory.HouseholdOwnerAsync();

        var response = await me.PostAsJsonAsync("/api/assistant/messages", new { message = "Hi", confirm = "x" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    private async Task<(string Token, JsonElement History)> HoldAsync(HttpClient me, string name)
    {
        _factory.Model.CallsTool("create_wallet", Wallet(name));
        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { message = "Create a wallet" });
        return (Data(events, "pending_action").GetProperty("token").GetString()!, Data(events, "history").GetProperty("messages"));
    }

    private static string HeldCallId(JsonElement history)
        => history.Deserialize<List<ChatMessage>>(AIJsonUtilities.DefaultOptions)!
            .SelectMany(m => m.Contents).OfType<FunctionCallContent>().Single().CallId;

    private static Dictionary<string, object?> Wallet(string name)
        => new() { ["name"] = name, ["institution"] = "XTB", ["kind"] = "Brokerage", ["currency"] = "PLN", ["visibility"] = "Private" };

    private static JsonElement Data(List<SseItem<string>> events, string type)
        => JsonDocument.Parse(events.First(e => e.EventType == type).Data).RootElement;
}
