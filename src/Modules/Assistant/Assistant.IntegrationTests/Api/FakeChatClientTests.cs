namespace Assistant.IntegrationTests.Api;

using System.Text.Json;
using Assistant.Api;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;

/// <summary>#650 — the scripted E2E model: preview a week, propose the import with the preview token, report the outcome.</summary>
public sealed class FakeChatClientTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>, IDisposable
{
    private readonly FakeChatClient _model = new(new FakeTimeProvider(new DateTimeOffset(2026, 10, 28, 9, 0, 0, TimeSpan.Zero)));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A user message becomes a preview of seven daily lunches of a recipe named after the message.</summary>
    [Fact]
    public async Task UserMessage_PreviewsAWeekNamedAfterTheMessage()
    {
        ChatResponse response = await _model.GetResponseAsync([new(ChatRole.User, "Porridge week")], cancellationToken: Ct);

        FunctionCallContent call = Call(response);
        call.Name.ShouldBe("preview_plan_import");
        JsonElement plan = JsonSerializer.SerializeToElement(call.Arguments!["plan"]);
        plan.GetProperty("recipes")[0].GetProperty("name").GetString().ShouldBe("Porridge week");
        JsonElement schedule = plan.GetProperty("schedule");
        schedule.GetArrayLength().ShouldBe(7);
        schedule[0].GetProperty("date").GetString().ShouldBe("2026-10-28");
        schedule[6].GetProperty("date").GetString().ShouldBe("2026-11-03");
    }

    /// <summary>A preview with a token is followed by the import proposal carrying that token and the same plan.</summary>
    [Fact]
    public async Task PreviewWithToken_ProposesTheImport()
    {
        List<ChatMessage> messages = [new(ChatRole.User, "Porridge week")];
        FunctionCallContent preview = Call(await _model.GetResponseAsync(messages, cancellationToken: Ct));
        messages.Add(new ChatMessage(ChatRole.Assistant, [preview]));
        messages.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(preview.CallId, """{"preview":{},"previewToken":"tok-1"}""")]));

        FunctionCallContent execute = Call(await _model.GetResponseAsync(messages, cancellationToken: Ct));

        execute.Name.ShouldBe("execute_plan_import");
        execute.Arguments!["previewToken"].ShouldBe("tok-1");
        execute.Arguments["plan"].ShouldBeSameAs(preview.Arguments!["plan"]);
    }

    /// <summary>After the import ran, was cancelled, or the preview failed, the model answers in text.</summary>
    /// <param name="tool">The call the newest result answers.</param>
    /// <param name="result">That result.</param>
    /// <param name="answer">Expected reply.</param>
    [Theory]
    [InlineData("execute_plan_import", """{"plannedMeals":7}""", "Done. The week is in your planner.")]
    [InlineData("execute_plan_import", "The user cancelled this action; nothing was changed.", "Okay, nothing was planned.")]
    [InlineData("preview_plan_import", """{"preview":{},"previewToken":null}""", "The plan could not be imported.")]
    public async Task ToolResult_AnswersWithTheOutcome(string tool, string result, string answer)
    {
        var call = new FunctionCallContent("c1", tool, new Dictionary<string, object?> { ["plan"] = new { } });

        ChatResponse response = await _model.GetResponseAsync(
            [new(ChatRole.User, "Week"), new(ChatRole.Assistant, [call]), new(ChatRole.Tool, [new FunctionResultContent("c1", result)])],
            cancellationToken: Ct);

        response.Text.ShouldBe(answer);
    }

    /// <summary>The fake must never answer real users: Production refuses to start with it.</summary>
    [Fact]
    public void Startup_FakeProviderInProduction_Fails()
    {
        using var factory = new AssistantApiFactory(databases, new Dictionary<string, string?> { ["Assistant:Provider"] = "Fake" });
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        Exception failure = Should.Throw<Exception>(() => production.Services);

        Messages(failure).ShouldContain(m => m.Contains("refused in Production", StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public void Dispose() => _model.Dispose();

    private static FunctionCallContent Call(ChatResponse response)
        => response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Single();

    private static IEnumerable<string> Messages(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
            yield return exception.Message;
    }
}
