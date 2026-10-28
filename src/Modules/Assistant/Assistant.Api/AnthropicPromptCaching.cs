namespace Assistant.Api;

using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

/// <summary>
/// Puts an Anthropic cache breakpoint after the first system message. Tools render before the system
/// prompt, so the tool definitions and the fixed instructions are cached together; the date line that
/// follows stays outside the cached prefix. Keeps the loop provider-neutral.
/// </summary>
/// <param name="inner">The Anthropic chat client.</param>
internal sealed class AnthropicPromptCaching(IChatClient inner) : DelegatingChatClient(inner)
{
    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetResponseAsync(MarkInstructions(messages), options, cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(MarkInstructions(messages), options, cancellationToken);

    private static List<ChatMessage> MarkInstructions(IEnumerable<ChatMessage> messages)
    {
        List<ChatMessage> list = [.. messages];
        list.FirstOrDefault(m => m.Role == ChatRole.System)?.Contents.LastOrDefault()?.WithCacheControl(new CacheControlEphemeral());
        return list;
    }
}
