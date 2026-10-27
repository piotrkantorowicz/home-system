namespace Shared.Testing.Mcp;

using System.Net.Http.Headers;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shared.Infrastructure.Web;

/// <summary>Connects MCP clients to a test host and reads tool results.</summary>
public static class McpTestClient
{
    /// <summary>
    /// Connects an MCP client as <paramref name="subject"/>. The first connect of a cold host can get a 499 from the
    /// handshake (#710), so a 499 is retried once.
    /// </summary>
    /// <param name="createClient">Creates an HTTP client addressing the host by <see cref="McpTestAuth.PublicBaseUrl"/>.</param>
    /// <param name="subject">The token subject.</param>
    /// <param name="scope">The token scope.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<McpClient> ConnectAsync(Func<HttpClient> createClient, string subject, string scope, CancellationToken ct)
    {
        // ponytail: one retry hides the cold-start 499 from the suite; Mcp499Probe logs its cause. Drop both once #710 is understood.
        try
        {
            return await ConnectOnceAsync(createClient, subject, scope, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException e) when ((int?)e.StatusCode == ApplicationExceptionHandler.ClientClosedRequestStatusCode)
        {
            Mcp499Probe.Log($"connect got 499, retrying once: {e.Message}");
            return await ConnectOnceAsync(createClient, subject, scope, ct).ConfigureAwait(false);
        }
    }

    /// <summary>The concatenated text content of a tool result.</summary>
    public static string Text(CallToolResult result)
        => string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static async Task<McpClient> ConnectOnceAsync(Func<HttpClient> createClient, string subject, string scope, CancellationToken ct)
    {
        var http = createClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", McpTestAuth.Token(subject, scope));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp"), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport, cancellationToken: ct).ConfigureAwait(false);
    }
}
