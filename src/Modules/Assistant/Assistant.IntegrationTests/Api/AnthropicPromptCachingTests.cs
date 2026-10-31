namespace Assistant.IntegrationTests.Api;

using Assistant.Api;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.AI;

/// <summary>Cache breakpoints: exactly two per call, on the instructions and the newest message, however many rounds resend the same content.</summary>
public sealed class AnthropicPromptCachingTests
{
    // Where the Anthropic SDK keeps the mark (AIContentCacheExtensions.CacheControlKey; its getter is internal).
    private const string CacheControlKey = "anthropic:cache_control";

    /// <summary>The second round moves the trailing breakpoint to the tool result and drops the first round's.</summary>
    [Fact]
    public async Task GetResponse_SecondRound_MarksInstructionsAndLastMessageOnly()
    {
        var model = new ScriptedChatClient();
        using var caching = new AnthropicPromptCaching(model);
        var instructions = new ChatMessage(ChatRole.System, "Instructions");
        var date = new ChatMessage(ChatRole.System, "Today");
        var question = new ChatMessage(ChatRole.User, "Wallets?") { Contents = { new TextContent("Stale mark from history").WithCacheControl(new Anthropic.Models.Messages.CacheControlEphemeral()) } };
        var call = new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("c1", "list_wallets")]);
        var result = new ChatMessage(ChatRole.Tool, [new FunctionResultContent("c1", "[]")]);

        await caching.GetResponseAsync([instructions, date, question], cancellationToken: TestContext.Current.CancellationToken);

        await caching.GetResponseAsync([instructions, date, question, call, result], cancellationToken: TestContext.Current.CancellationToken);

        AIContent[] marked = [.. new[] { instructions, date, question, call, result }.SelectMany(m => m.Contents).Where(IsMarked)];
        marked.ShouldBe([instructions.Contents[^1], result.Contents[^1]]);
    }

    private static bool IsMarked(AIContent content)
        => content.AdditionalProperties?.TryGetValue(CacheControlKey, out object? value) == true && value is not null;
}
