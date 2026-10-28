namespace Assistant.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

/// <summary>
/// The model, scripted: each call takes the next queued response (a plain "ok" when the queue is empty)
/// and records what the agent sent, so tests can see the offered tools and the tool results.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    private readonly ConcurrentQueue<Func<ChatResponse>> _script = new();

    /// <summary>Every call the agent made, in order.</summary>
    public ConcurrentQueue<(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options)> Calls { get; } = new();

    /// <summary>Queues a model turn that calls one tool.</summary>
    /// <param name="tool">Tool name.</param>
    /// <param name="arguments">Tool arguments.</param>
    public void CallsTool(string tool, Dictionary<string, object?>? arguments = null)
        => _script.Enqueue(() => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent($"call-{Guid.CreateVersion7():N}", tool, arguments ?? [])])));

    /// <summary>Queues a model turn that answers with text.</summary>
    /// <param name="text">The answer.</param>
    public void Answers(string text) => _script.Enqueue(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    /// <summary>Queues a model turn that fails like an unreachable provider.</summary>
    public void Fails() => _script.Enqueue(() => throw new HttpRequestException("provider down"));

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Next(messages, options));

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        foreach (ChatResponseUpdate update in Next(messages, options).ToChatResponseUpdates())
            yield return update;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private ChatResponse Next(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        Calls.Enqueue(([.. messages], options));
        ChatResponse response = _script.TryDequeue(out Func<ChatResponse>? next) ? next() : new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
        response.Usage = new UsageDetails { InputTokenCount = 100, OutputTokenCount = 10 };
        return response;
    }
}
