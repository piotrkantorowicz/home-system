namespace Shared.Infrastructure.Web;

using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
/// Calls the tools registered on the host's MCP server in-process, without the <c>/mcp</c> transport:
/// same catalog (names, descriptions, schemas, annotations), same tool methods, same results.
/// It does not apply the tools' MCP scope policies; the caller decides which tools a user may run.
/// </summary>
public sealed partial class McpToolInvoker : IAsyncDisposable
{
    private readonly Dictionary<string, McpServerTool> _tools;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<McpToolInvoker> _logger;
    private readonly McpToolMetrics _metrics;

    // ponytail: never-run server; RequestContext demands one but the tools do not talk to the client.
    // Replace with a real session if a tool ever needs sampling, elicitation or progress.
    private readonly McpServer _server;

    /// <summary>Creates the invoker over the host's registered tools.</summary>
    /// <param name="tools">Every tool registered on the MCP server.</param>
    /// <param name="scopes">Creates one DI scope per call, as the transport does per request.</param>
    /// <param name="services">Root services the tools resolve against.</param>
    /// <param name="logger">Logs unexpected tool failures; <c>/mcp</c> call logging does not run on this path.</param>
    /// <param name="metrics">Records each call's duration and outcome, as <c>/mcp</c> call logging does for that path.</param>
    public McpToolInvoker(
        IEnumerable<McpServerTool> tools, IServiceScopeFactory scopes, IServiceProvider services, ILogger<McpToolInvoker> logger, McpToolMetrics metrics)
    {
        _tools = tools.ToDictionary(t => t.ProtocolTool.Name, StringComparer.Ordinal);
        _scopes = scopes;
        _logger = logger;
        _metrics = metrics;
        _server = McpServer.Create(new StreamServerTransport(Stream.Null, Stream.Null), new McpServerOptions(), serviceProvider: services);
        Tools = [.. _tools.Values.Select(t => t.ProtocolTool).OrderBy(t => t.Name, StringComparer.Ordinal)];
    }

    /// <summary>The tool catalog, as <c>tools/list</c> describes it.</summary>
    public IReadOnlyList<Tool> Tools { get; }

    /// <summary>Runs one tool as <paramref name="user"/>.</summary>
    /// <param name="user">The authenticated caller; tools read identity only from it.</param>
    /// <param name="name">Tool name from <see cref="Tools"/>.</param>
    /// <param name="arguments">Tool arguments as JSON, as an MCP client would send them.</param>
    /// <param name="ct">Cancels the call.</param>
    /// <returns>The tool result; expected tool errors come back with <c>IsError</c> set, as over <c>/mcp</c>.</returns>
    public async Task<CallToolResult> InvokeAsync(
        ClaimsPrincipal user, string name, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);

        var started = Stopwatch.GetTimestamp();
        var outcome = McpToolMetrics.Error;
        try
        {
            CallToolResult result = await CallAsync(user, name, arguments, ct).ConfigureAwait(false);
            outcome = result.IsError == true ? McpToolMetrics.Error : McpToolMetrics.Success;
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            outcome = McpToolMetrics.Cancelled;
            throw;
        }
        finally
        {
            _metrics.RecordCall(name, outcome, Stopwatch.GetElapsedTime(started));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _server.DisposeAsync();

    private async Task<CallToolResult> CallAsync(
        ClaimsPrincipal user, string name, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken ct)
    {
        if (!_tools.TryGetValue(name, out McpServerTool? tool))
            return Error($"Unknown tool '{name}'.");

        await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<McpToolUser>().Set(user);

        var request = new CallToolRequestParams { Name = name, Arguments = arguments?.ToDictionary() };
        var context = new RequestContext<CallToolRequestParams>(_server, new JsonRpcRequest { Method = RequestMethods.ToolsCall }, request)
        {
            Services = scope.ServiceProvider,
            User = user,
        };

        // Mirrors the SDK's tools/call handling so a bad argument is a result the model can correct, not a crash.
        try
        {
            return await tool.InvokeAsync(context, ct).ConfigureAwait(false);
        }
        catch (McpException ex)
        {
            return Error($"An error occurred invoking '{name}': {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            LogFailure(_logger, name, ex);
            return Error($"An error occurred invoking '{name}'.");
        }
    }

    private static CallToolResult Error(string message)
        => new() { IsError = true, Content = [new TextContentBlock { Text = message }] };

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "In-process MCP tool {Tool} failed")]
    private static partial void LogFailure(ILogger logger, string tool, Exception exception);
}
