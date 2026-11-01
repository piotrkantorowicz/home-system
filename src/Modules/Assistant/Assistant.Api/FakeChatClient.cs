namespace Assistant.Api;

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

/// <summary>
/// <c>Assistant:Provider = Fake</c>: a scripted model for E2E runs, refused in Production (design §7).
/// It plays one scenario through the real tools: every user message asks to plan a week of lunches of
/// a recipe named after the message; it previews the plan, proposes the import (held for the user's
/// Confirm) with the preview token, then reports the outcome. No network, no cost.
/// </summary>
/// <param name="clock">Dates the week from today.</param>
internal sealed class FakeChatClient(TimeProvider clock) : IChatClient
{
    private const string PreviewTool = "preview_plan_import";
    private const string ExecuteTool = "execute_plan_import";
    private const int DaysInWeek = 7;
    private const int MaxNameLength = 60;

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Next([.. messages]));

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        foreach (ChatResponseUpdate update in Next([.. messages]).ToChatResponseUpdates())
            yield return update;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("fake", defaultModelId: "fake") : null;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private ChatResponse Next(List<ChatMessage> messages)
    {
        ChatMessage last = messages[^1];
        if (last.Role == ChatRole.User)
            return Call(PreviewTool, new() { ["plan"] = Plan(last.Text), ["appendToOccupiedSlots"] = true });

        // Which call the newest result answers decides the next step.
        FunctionResultContent? result = last.Contents.OfType<FunctionResultContent>().LastOrDefault();
        string text = result?.Result?.ToString() ?? "";
        FunctionCallContent? call = messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
            .LastOrDefault(c => c.CallId == result?.CallId);

        return call?.Name switch
        {
            PreviewTool when PreviewToken(text) is { } token && call.Arguments is { } arguments => Call(ExecuteTool, new()
            {
                ["plan"] = arguments["plan"],
                ["previewToken"] = token,
                ["operationKey"] = "replaced-by-the-server",
                ["appendToOccupiedSlots"] = true,
            }),
            ExecuteTool when text.Contains("cancelled", StringComparison.OrdinalIgnoreCase) => Answer("Okay, nothing was planned."),
            ExecuteTool => Answer("Done. The week is in your planner."),
            _ => Answer("The plan could not be imported."),
        };
    }

    // A self-contained week: one product, one recipe, lunch every day from today.
    private JsonObject Plan(string message)
    {
        string recipe = message.Trim() is { Length: > 0 } text ? text[..Math.Min(text.Length, MaxNameLength)] : "Assistant week";
        string product = $"{recipe} base";
        DateOnly today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var schedule = new JsonArray();
        for (var day = 0; day < DaysInWeek; day++)
        {
            schedule.Add(new JsonObject
            {
                ["date"] = today.AddDays(day).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["meals"] = new JsonArray(new JsonObject { ["type"] = "lunch", ["recipe"] = recipe, ["servings"] = 1 }),
            });
        }

        return new JsonObject
        {
            ["products"] = new JsonArray(new JsonObject { ["name"] = product, ["caloriesPer100g"] = 150, ["unit"] = "g" }),
            ["recipes"] = new JsonArray(new JsonObject
            {
                ["name"] = recipe,
                ["servings"] = 1,
                ["ingredients"] = new JsonArray(new JsonObject { ["product"] = product, ["amount"] = 100, ["unit"] = "g" }),
            }),
            ["schedule"] = schedule,
        };
    }

    private static string? PreviewToken(string result)
    {
        try
        {
            return JsonNode.Parse(result)?["previewToken"]?.GetValue<string>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ChatResponse Call(string tool, Dictionary<string, object?> arguments)
        => Usage(new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent($"fake-{Guid.CreateVersion7():N}", tool, arguments)])));

    private static ChatResponse Answer(string text) => Usage(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    private static ChatResponse Usage(ChatResponse response)
    {
        response.Usage = new UsageDetails { InputTokenCount = 0, OutputTokenCount = 0 };
        return response;
    }
}
