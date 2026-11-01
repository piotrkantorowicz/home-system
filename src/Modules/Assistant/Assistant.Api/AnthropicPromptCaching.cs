namespace Assistant.Api;

using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

/// <summary>
/// Places two Anthropic cache breakpoints on every call: after the first system message and after the
/// last message. Tools render before the system prompt, so the first caches the tool definitions with
/// the fixed instructions (the date line that follows stays outside it); the second lets each tool round
/// read the previous round's history and tool results from cache. Keeps the loop provider-neutral.
/// </summary>
/// <param name="inner">The Anthropic chat client.</param>
internal sealed class AnthropicPromptCaching(IChatClient inner) : DelegatingChatClient(inner)
{
    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetResponseAsync(MarkBreakpoints(messages), options, cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(MarkBreakpoints(messages), options, cancellationToken);

    private static List<ChatMessage> MarkBreakpoints(IEnumerable<ChatMessage> messages)
    {
        List<ChatMessage> list = [.. messages];

        // The loop resends the same content objects every round and the browser history is untrusted,
        // so clear every earlier mark first: the API rejects more than four breakpoints.
        // A typed null: a bare null literal matches both WithCacheControl overloads (CS0121).
        CacheControlEphemeral? none = null;
        foreach (AIContent content in list.SelectMany(m => m.Contents))
            content.WithCacheControl(none);

        list.FirstOrDefault(m => m.Role == ChatRole.System)?.Contents.LastOrDefault()?.WithCacheControl(new CacheControlEphemeral());
        list.LastOrDefault()?.Contents.LastOrDefault()?.WithCacheControl(new CacheControlEphemeral());
        return list;
    }
}
