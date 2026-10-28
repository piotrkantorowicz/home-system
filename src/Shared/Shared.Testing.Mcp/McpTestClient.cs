namespace Shared.Testing.Mcp;

using System.Net.Http.Headers;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

/// <summary>Connects MCP clients to a test host and reads tool results.</summary>
public static class McpTestClient
{
    /// <summary>Connects an MCP client as <paramref name="subject"/>.</summary>
    /// <param name="createClient">Creates an HTTP client addressing the host by <see cref="McpTestAuth.PublicBaseUrl"/>.</param>
    /// <param name="subject">The token subject.</param>
    /// <param name="scope">The token scope.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<McpClient> ConnectAsync(Func<HttpClient> createClient, string subject, string scope, CancellationToken ct)
    {
        var http = createClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", McpTestAuth.Token(subject, scope));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp"), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            ownsHttpClient: true);

        // The server/discover probe gives up after 5 s by default, and a cold test host can take longer on its first
        // request: the client then cancels it (a 499) and its fallback handshake can fail (#710). The 60 s
        // InitializationTimeout still bounds the connect.
        var options = new McpClientOptions { DiscoverProbeTimeout = Timeout.InfiniteTimeSpan };
        return await McpClient.CreateAsync(transport, options, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>The concatenated text content of a tool result.</summary>
    public static string Text(CallToolResult result)
        => string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));
}
