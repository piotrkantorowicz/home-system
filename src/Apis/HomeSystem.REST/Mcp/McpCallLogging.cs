namespace HomeSystem.REST.Mcp;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
/// Logs every tool call with its name, outcome, duration and the HTTP trace identifier. Arguments,
/// results, notes and tokens are never logged.
/// </summary>
internal static partial class McpCallLogging
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Filter(McpRequestHandler<CallToolRequestParams, CallToolResult> next)
        => async (context, ct) =>
        {
            var logger = context.Services!.GetRequiredService<ILoggerFactory>().CreateLogger("HomeSystem.Mcp");
            var tool = context.Params?.Name ?? "?";
            var traceId = Activity.Current?.TraceId.ToString() ?? "-";
            var started = Stopwatch.GetTimestamp();
            string outcome = "ok";
            try
            {
                var result = await next(context, ct);
                if (result.IsError == true)
                    outcome = "error";
                return result;
            }
            catch (Exception ex)
            {
                outcome = ex.GetType().Name;
                throw;
            }
            finally
            {
                var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                LogCall(logger, tool, outcome, elapsedMs, traceId);
            }
        };

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "MCP tool {Tool} finished with {Outcome} in {ElapsedMs:0} ms (trace {TraceId})")]
    private static partial void LogCall(ILogger logger, string tool, string outcome, double elapsedMs, string traceId);
}
