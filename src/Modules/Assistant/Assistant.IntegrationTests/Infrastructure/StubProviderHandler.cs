namespace Assistant.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using System.Net;
using System.Text;

/// <summary>
/// Stands in for an OpenAI-compatible provider on the wire: answers every chat completion with a streamed
/// "Hi" and a usage of 40 input / 2 output tokens, and records each request. No network.
/// </summary>
public sealed class StubProviderHandler : HttpMessageHandler
{
    private const string Stream = """
        data: {"id":"c1","object":"chat.completion.chunk","created":0,"model":"test-model","choices":[{"index":0,"delta":{"role":"assistant","content":"Hi"},"finish_reason":null}]}

        data: {"id":"c1","object":"chat.completion.chunk","created":0,"model":"test-model","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}

        data: {"id":"c1","object":"chat.completion.chunk","created":0,"model":"test-model","choices":[],"usage":{"prompt_tokens":40,"completion_tokens":2,"total_tokens":42}}

        data: [DONE]


        """;

    /// <summary>Every request sent, with its body.</summary>
    public ConcurrentQueue<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue((request, request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Stream.ReplaceLineEndings("\n"), Encoding.UTF8, "text/event-stream"),
        };
    }
}
